using System;
using System.Collections.Generic;

namespace BeanTrader.Models
{
    public class TradeOffer
    {
        public Guid Id { get; set; }
        public Guid SellerId { get; set; }
        public Dictionary<Beans, uint> Offering { get; set; }
        public Dictionary<Beans, uint> Asking { get; set; }
    }
}
