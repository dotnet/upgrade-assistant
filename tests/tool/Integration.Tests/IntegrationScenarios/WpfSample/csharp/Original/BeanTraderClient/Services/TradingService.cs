using BeanTrader.Models;
using BeanTraderClient.DependencyInjection;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BeanTraderClient.Services
{
    public sealed class TradingService : IDisposable
    {
        private readonly AsyncLock clientSyncLock = new AsyncLock();
        private BeanTraderServiceClient client;

        private CancellationTokenSource CancellationSource { get; }
        private BeanTraderServiceClientFactory ClientFactory { get; }

        private async Task<BeanTraderServiceClient> GetOrOpenClientAsync()
        {
            // If the client does not exist or is in a bad state, re-create it
            if (client == null || client.State == CommunicationState.Closed || client.State == CommunicationState.Faulted)
            {
                try
                {
                    using (await clientSyncLock.LockAsync())
                    {
                        if (client == null || client.State == CommunicationState.Closed || client.State == CommunicationState.Faulted)
                        {
                            var newClient = ClientFactory.GetServiceClient();
                            SetClientCredentials(newClient);
                            newClient.Open();
                            client = newClient;
                        }
                    }

                    Connected?.Invoke();
                }
                catch (EndpointNotFoundException)
                {
                    MessageBox.Show("Failed to connect to Bean Trader service. Make sure BeanTraderServer.exe is running");
                    throw;
                }
            }

            return client;
        }

        // Used by callers to be notified when the BeanTraderService connection is (re)-established
        // If the connection was dropped, callers may want to use this event to be notified
        // that they should query for any data that may have been missed while disconnected.
        public event Func<Task> Connected;

        public TradingService(BeanTraderServiceClientFactory clientFactory)
        {
            ClientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            CancellationSource = new CancellationTokenSource();

            // Begin monitoring the connection for faults or disconnects
            Task.Run(() => CheckForHeartbeatAsync(CancellationSource.Token));
        }

        public Task<bool> AcceptTradeAsync(Guid id) =>
            SafeServiceCallAsync(async () =>
            {
                var client = await GetOrOpenClientAsync();
                return await client.AcceptTradeAsync(id);
            });

        public Task<bool> CancelTradeOfferAsync(Guid id) =>
            SafeServiceCallAsync(async () =>
            {
                var client = await GetOrOpenClientAsync();
                return await client.CancelTradeOfferAsync(id);
            });

        public Task<Trader> GetCurrentTraderInfoAsync() =>
            SafeServiceCallAsync(async () =>
            {
                var client = await GetOrOpenClientAsync();
                return await client.GetCurrentTraderInfoAsync();
            });

        public Task<Dictionary<Guid, string>> GetTraderNamesAsync(Guid[] ids) =>
            SafeServiceCallAsync(async () =>
            {
                var client = await GetOrOpenClientAsync();
                return await client.GetTraderNamesAsync(ids);
            });

        public Task<TradeOffer[]> ListenForTradeOffersAsync() =>
            SafeServiceCallAsync(async () =>
            {
                var client = await GetOrOpenClientAsync();
                return await client.ListenForTradeOffersAsync();
            });

        public Task LoginAsync(string name) =>
            SafeServiceCallAsync(async () =>
            {
                var client = await GetOrOpenClientAsync();
                await client.LoginAsync(name);
            });

        public Task LogoutAsync() =>
            SafeServiceCallAsync(async () =>
            {
                var client = await GetOrOpenClientAsync();
                await client.LogoutAsync();
            });

        public Task<Guid> OfferTradeAsync(TradeOffer trade) =>
            SafeServiceCallAsync(async () =>
            {
                var client = await GetOrOpenClientAsync();
                return await client.OfferTradeAsync(trade);
            });

        public Task StopListeningAsync() =>
            SafeServiceCallAsync(async () =>
            {
                var client = await GetOrOpenClientAsync();
                await client.StopListeningAsync();
            });

        private async Task CheckForHeartbeatAsync(CancellationToken ct)
        {
            var emptyInput = Array.Empty<Guid>();
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await GetTraderNamesAsync(emptyInput).ConfigureAwait(false);
                    await Task.Delay(5000, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }
        }

        private async Task<T> SafeServiceCallAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (CommunicationException)
            {
                client?.Abort();
                client = null;
                return default;
            }
        }

        private async Task SafeServiceCallAsync(Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (CommunicationException)
            {
                client?.Abort();
                client = null;
            }
        }

        private static void SetClientCredentials(BeanTraderServiceClient client)
        {
            client.ClientCredentials.ClientCertificate.Certificate = GetCertificate();
            client.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
        }

        private static X509Certificate2 GetCertificate()
        {
            var certPath = Path.Combine(Path.GetDirectoryName(typeof(TradingService).Assembly.Location), "BeanTrader.pfx");
            return new X509Certificate2(certPath, "password");
        }

        public void Dispose()
        {
            CancellationSource.Cancel();
            try
            {
                client?.Close();
            }
            catch (CommunicationException)
            {
                client?.Abort();
            }
        }
    }
}
