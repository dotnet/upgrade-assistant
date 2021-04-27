// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class UpgradeContextProperties : IUpgradeContextProperties
    {
        private readonly Dictionary<string, (bool Persistent, string PropertyValue)> _propertyStorage;

        public UpgradeContextProperties()
        {
            _propertyStorage = new Dictionary<string, (bool, string)>();
        }

        public string? GetPropertyValue(string propertyName)
        {
            if (_propertyStorage.TryGetValue(propertyName, out var value))
            {
                return value.PropertyValue;
            }
            else
            {
                return null;
            }
        }

        public void SetPropertyValue(string propertyName, string value, bool persistent)
        {
            _propertyStorage[propertyName] = (persistent, value);
        }

        public bool RemovePropertyValue(string propertyName)
        {
            return _propertyStorage.Remove(propertyName);
        }

        public IEnumerable<KeyValuePair<string, string>> GetAllPropertyValues()
        {
            return _propertyStorage.Select(i => new KeyValuePair<string, string>(i.Key, i.Value.PropertyValue)).ToList();
        }

        public IEnumerable<KeyValuePair<string, string>> GetPersistentPropertyValues()
        {
            return _propertyStorage.Where(p => p.Value.Persistent)
                .Select(i => new KeyValuePair<string, string>(i.Key, i.Value.PropertyValue)).ToList();
        }
    }
}
