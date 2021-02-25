using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace BeanTrader.Models
{
    // This is only needed because WPF DataTemplates can't format generic types
    [Serializable]
    public class BeanDictionary : Dictionary<Beans, uint>
    {
        public BeanDictionary(Dictionary<Beans, uint> source) : base(source) { }

        public BeanDictionary(): this(new Dictionary<Beans, uint>())
        {
            foreach (var bean in Enum.GetValues(typeof(Beans)).Cast<Beans>())
            {
                Add(bean, 0);
            }
        }

        protected BeanDictionary(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        { }
    }
}
