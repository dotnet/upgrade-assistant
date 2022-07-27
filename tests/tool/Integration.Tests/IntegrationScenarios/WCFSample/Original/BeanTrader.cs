using BeanTrader;
using BeanTrader.Models;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace BeanTraderServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    class BeanTrader : IBeanTrader
    {

        // Traders (organized by UserId)
        private ConcurrentDictionary<Guid, Trader> Traders { get; }

        // Open trades (organized by TradeId)
        private ConcurrentDictionary<Guid, TradeOffer> TradeOffers { get; }

        // Active listening sessions (organized by SessionId)
        private ConcurrentDictionary<string, IBeanTraderCallback> Callbacks { get; }

        // Active Sessions (organized by SessionId)
        private ConcurrentDictionary<string, Guid> LoggedInTraders { get; }

        private readonly HashAlgorithm Algorithm = SHA1.Create();

        public BeanTrader()
        {
            TradeOffers = new ConcurrentDictionary<Guid, TradeOffer>();
            Traders = new ConcurrentDictionary<Guid, Trader>();
            Callbacks = new ConcurrentDictionary<string, IBeanTraderCallback>();
            LoggedInTraders = new ConcurrentDictionary<string, Guid>();
        }

        public IEnumerable<TradeOffer> ListenForTradeOffers()
        {
            var context = OperationContext.Current;
            var sessionId = context.SessionId;
            var user = GetCurrentTraderInfo();

            if (user == null)
            {
                Log.Warning("Failed to return trade offers for session {SessionId} because no user is logged on", sessionId);
                return Enumerable.Empty<TradeOffer>();
            }

            var callback = context.GetCallbackChannel<IBeanTraderCallback>();
            Callbacks.AddOrUpdate(sessionId, callback, (id, oldCallback) => callback);

            Log.Information("User {UserId} has begun listening for trade offers in session {SessionId}", user.Id, sessionId);
            Log.Information("{ListenerCount} traders are listening", Callbacks.Count);

            Log.Information("Sending ~{OfferCount} trade offers to {UserId}", TradeOffers.Count, user.Id);

            // Make sure to get TradeOffers after registering the callback so that the new listener
            // doesn't miss trades made in between the two events.
            return TradeOffers.Values;
        }

        public bool AcceptTrade(Guid offerId)
        {
            var buyer = GetCurrentTraderInfo();

            if (buyer == null)
            {
                Log.Warning("Failed to accept trade offer {TradeOfferId} for session {SessionId} because no user is logged on", offerId, OperationContext.Current.SessionId);
            }

            if (!TradeOffers.TryGetValue(offerId, out TradeOffer tradeOffer))
            {
                Log.Information("Trader {UserId} attempted to accept trade offer {TradeOfferId} but it was not available", buyer.Id, offerId);
                return false;
            }

            if (!Traders.TryGetValue(tradeOffer.SellerId, out Trader seller))
            {
                Log.Information("Trader {UserId} attempted to accept trade offer {TradeOfferId} but seller ({UserId}) was not found.", buyer.Id, offerId, tradeOffer.SellerId);
                return false;
            }

            // Lock the buyer to make sure that they don't make multiple transactions at once, potentially overspending
            lock(buyer)
            {
                if (!TraderHasFunds(buyer, tradeOffer.Asking))
                {
                    Log.Information("Trader {UserId} attempted to accept trade offer {TradeOfferId} but had insufficient funds", buyer.Id, offerId);
                    return false;
                }

                // If all else checks out (buyer funds, seller presence, etc.), attempt to grab the trade offer and remove
                // it from availabilty
                if (!TradeOffers.TryRemove(offerId, out tradeOffer))
                {
                    Log.Information("Trader {UserId} attempted to accept trade offer {TradeOfferId} but it was not available (2)", buyer.Id, offerId);
                    return false;
                }

                // Transfer beans
                RemoveBeans(buyer, tradeOffer.Asking);
            }

            // Beans can be added without locking. Nested locks for the buyer and seller would cause deadlocks.
            AddBeans(buyer, tradeOffer.Offering);
            AddBeans(seller, tradeOffer.Asking);
            Log.Information("Trader {UserId} has accepted trade offer {TradeOfferId} from seller {UserId}", buyer.Id, offerId, tradeOffer.SellerId);

            // Alert traders that the trade has been accepted and is no longer available
            var invalidCallbacks = new List<string>();
            foreach (var callback in Callbacks)
            {
                try
                {
                    callback.Value?.TradeAccepted(tradeOffer, buyer.Id);
                    callback.Value?.RemoveTradeOffer(offerId);
                }
                catch (CommunicationException)
                {
                    Log.Warning("Session {SessionId}'s channel closed unexpectedly; will remove from callback list", callback.Key);
                    invalidCallbacks.Add(callback.Key);
                }
            }
            foreach (var id in invalidCallbacks)
            {
                Callbacks.TryRemove(id, out _);
            }

            return true;
        }

        public Guid OfferTrade(TradeOffer tradeOffer)
        {
            var seller = GetCurrentTraderInfo();

            if (seller == null)
            {
                Log.Warning("Failed to offer trade for session {SessionId} because no user is logged on", OperationContext.Current.SessionId);
                return Guid.Empty;
            }

            if (tradeOffer == null || tradeOffer.Offering == null || tradeOffer.Asking == null)
            {
                Log.Information("Rejecting invalid trade offer from {UserId}", seller.Id);
                return Guid.Empty;
            }

            tradeOffer.Id = Guid.NewGuid();
            tradeOffer.SellerId = seller.Id;
            
            lock(seller)
            {
                if (!TraderHasFunds(seller, tradeOffer.Offering))
                {
                    Log.Information("Trade offer rejected because user {UserId} had insufficient funds", seller.Id);
                    return Guid.Empty;
                }

                // Hold seller's beans
                RemoveBeans(seller, tradeOffer.Offering);
            }

            // Post the trade (before broadcasting to avoid a narror race condition)
            TradeOffers.AddOrUpdate(tradeOffer.Id, tradeOffer, (id, oldOffer) => tradeOffer);
            Log.Information("New trade offer ({TradeOfferId}) listed by {UserId}", tradeOffer.Id, seller.Id);

            List<string> invalidCallbacks = new List<string>();
            foreach (var callback in Callbacks)
            {
                try
                {
                    callback.Value?.AddNewTradeOffer(tradeOffer);
                }
                catch(CommunicationException)
                {
                    Log.Warning("Session {SessionId}'s channel closed unexpectedly; will remove from callback list", callback.Key);
                    invalidCallbacks.Add(callback.Key);
                }
            }
            foreach (var id in invalidCallbacks)
            {
                Callbacks.TryRemove(id, out _);
            }

            return tradeOffer.Id;
        }

        public bool CancelTradeOffer(Guid offerId)
        {
            var seller = GetCurrentTraderInfo();

            if (seller == null)
            {
                Log.Warning("Failed to cancel trade {TradeOfferId} for session {SessionId} because no user is logged on", offerId, OperationContext.Current.SessionId);
                return false;
            }

            if (TradeOffers.TryRemove(offerId, out TradeOffer tradeOffer))
            {
                List<string> invalidCallbacks = new List<string>();
                foreach (var callback in Callbacks)
                {
                    try
                    {
                        callback.Value?.RemoveTradeOffer(offerId);
                    }
                    catch (CommunicationException)
                    {
                        Log.Warning("Session {SessionId}'s channel closed unexpectedly; will remove from callback list", callback.Key);
                        invalidCallbacks.Add(callback.Key);
                    }
                }
                foreach (var id in invalidCallbacks)
                {
                    Callbacks.TryRemove(id, out _);
                }

                // Refund seller's beans
                AddBeans(seller, tradeOffer.Offering);

                Log.Information("Trade {TradeOfferId} cancelled", offerId);

                return true;
            }

            Log.Information("Trade {TradeOfferId} could not be removed because it was not available", offerId);

            return false;
        }

        public void StopListening()
        {
            var sessionId = OperationContext.Current.SessionId;
            var userId = GetUserIdForContext(OperationContext.Current);
            Log.Information("Session {SessionId} (user {UserId}) has stopped listening", sessionId, userId);
            Callbacks.TryRemove(sessionId, out _);
            Log.Information("{ListenerCount} traders are listening for trade offers", Callbacks.Count);
        }

        public Trader GetCurrentTraderInfo()
        {
            var userId = GetUserIdForContext(OperationContext.Current);
            return userId.HasValue ?
                Traders.GetOrAdd(userId.Value, new Trader(userId.Value)) :
                Trader.Empty;
        }

        public void Login(string name)
        {
            // For demo purposes, just convert requested user name to user ID
            // without authentication.
            var userId = ConvertStringToGuid(name);
            LoggedInTraders.AddOrUpdate(OperationContext.Current.SessionId, userId, (s, g) => userId);
            var user = GetCurrentTraderInfo();
            user.Name = name;

            Log.Information("User {UserId} ({UserName}) logged in", user.Id, user.Name);
        }

        public void Logout()
        {
            var user = GetCurrentTraderInfo();
            if (user == null)
            {
                return;
            }

            if (LoggedInTraders.TryRemove(OperationContext.Current.SessionId, out _))
            {
                Log.Information("User {UserId} ({UserName}) logged out", user.Id, user.Name);
            }
            else
            {
                Log.Information("Unable to log out user {UserId} ({UserName}) because they were not logged in", user.Id, user.Name);
            }
        }

        public Dictionary<Guid, string> GetTraderNames(IEnumerable<Guid> traderIds)
        {
            var currentUser = GetCurrentTraderInfo();
            var ret = new Dictionary<Guid, string>();

            foreach (var traderId in traderIds.Distinct())
            {
                if (Traders.TryGetValue(traderId, out Trader trader))
                {
                    ret.Add(traderId, trader.Name);
                }
            }

            Log.Information("Retrieved {count} trader names for user {UserId}", ret.Count, currentUser.Id);
            return ret;
        }
        
        private Guid? GetUserIdForContext(OperationContext context)
        {
            if (LoggedInTraders.TryGetValue(context.SessionId, out Guid userId))
            {
                return userId;
            }
            else
            {
                return null;
            }
        }

        private bool TraderHasFunds(Trader buyer, IEnumerable<KeyValuePair<Beans, uint>> beans)
        {
            foreach (var beanCount in beans)
            {
                if (buyer.Inventory[(int)beanCount.Key] < beanCount.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private void AddBeans(Trader trader, IEnumerable<KeyValuePair<Beans, uint>> beans) => AdjustBeans(trader, beans, true);

        private void RemoveBeans(Trader trader, IEnumerable<KeyValuePair<Beans, uint>> beans) => AdjustBeans(trader, beans, false);

        private void AdjustBeans(Trader trader, IEnumerable<KeyValuePair<Beans, uint>> beans, bool addBeans)
        {
            foreach (var beanCount in beans)
            {
                var newCount = Interlocked.Add(ref trader.Inventory[(int)beanCount.Key], ((int)beanCount.Value) * (addBeans ? 1 : -1));
                if (newCount < 0)
                {
                    Log.Error("Trader {UserId} has negative inventory ({BeanCount} {BeanType}s)", trader.Id, newCount, beanCount.Key.ToString());
                }
            }
        }


        private Guid ConvertStringToGuid(string userId)
        {
            var inputBytes = Encoding.UTF8.GetBytes(userId);
            var bytes = Algorithm.ComputeHash(inputBytes);
            var truncatedBytes = new byte[16];
            Array.Copy(bytes, 0, truncatedBytes, 0, 16);
            return new Guid(truncatedBytes);
        }
    }
}
