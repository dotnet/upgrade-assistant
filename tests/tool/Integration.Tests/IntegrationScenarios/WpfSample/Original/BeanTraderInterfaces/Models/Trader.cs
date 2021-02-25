using System;
using System.Linq;
using System.Runtime.Serialization;

namespace BeanTrader.Models
{
    [DataContract]
    public class Trader
    {
        static int[] DefaultBeans = new[] { 100, 50, 10, 1 };

        public static Trader Empty = new Trader(Guid.Empty, string.Empty)
        {
            Inventory = new int[Enum.GetValues(typeof(Beans)).Cast<int>().Max() + 1]
        };

        public Trader(): this(Guid.NewGuid()) { }

        public Trader(Guid id) : this(id, $"Trader {id}") { }

        public Trader(string name) : this(Guid.NewGuid(), name) { }

        public Trader(Guid id, string name)
        {
            Id = id;
            Name = name;
            Inventory = new int[Enum.GetValues(typeof(Beans)).Cast<int>().Max() + 1];
            Array.Copy(DefaultBeans, Inventory, DefaultBeans.Length);
        }

        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int[] Inventory { get; set; }
    }
}
