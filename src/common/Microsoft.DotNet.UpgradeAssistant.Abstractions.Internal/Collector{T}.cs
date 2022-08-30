// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class Collector<T> : ICollector<T>
    {
        private readonly List<T> _list = new List<T>();

        public void Add(T item)
        {
            _list.Add(item);
        }

        public void AddRange(IEnumerable<T> items)
        {
            _list.AddRange(items);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
