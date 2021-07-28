// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    internal class DependencyCollection<T> : IDependencyCollection<T>
    {
        private readonly IEnumerable<Operation<T>> _initial;
        private readonly Action<BuildBreakRisk> _setRisk;

        public DependencyCollection(IEnumerable<T> initial, Action<BuildBreakRisk> setRisk)
        {
            _initial = initial.Select(i => new Operation<T>(i, new()));
            _setRisk = setRisk;
        }

        public HashSet<Operation<T>> Additions { get; } = new HashSet<Operation<T>>();

        public HashSet<Operation<T>> Deletions { get; } = new HashSet<Operation<T>>();

        public bool Contains(T item)
        {
            if (_initial.Any(i => i.Item != null && i.Item.Equals(item)))
            {
                return true;
            }

            return false;
        }

        bool IDependencyCollection<T>.Add(T item, OperationDetails od)
        {
            var operation = new Operation<T>(item, od);

            if (Contains(item))
            {
                return false;
            }

            if (Additions.Add(operation))
            {
                _setRisk(od.Risk);
                return true;
            }

            return false;
        }

        bool IDependencyCollection<T>.Remove(T item, OperationDetails od)
        {
            var operation = new Operation<T>(item, od);

            if (!Contains(item))
            {
                return false;
            }

            if (Deletions.Add(operation))
            {
                _setRisk(od.Risk);
                return true;
            }

            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in _initial)
            {
                if (!Deletions.Contains(item))
                {
                    yield return item.Item;
                }
            }

            foreach (var additions in Additions)
            {
                yield return additions.Item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Add(T item, OperationDetails details)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item, OperationDetails details)
        {
            throw new NotImplementedException();
        }

        public bool HasChanges => Additions.Any() || Deletions.Any();

        IReadOnlyCollection<Operation<T>> IDependencyCollection<T>.Additions => Additions;

        IReadOnlyCollection<Operation<T>> IDependencyCollection<T>.Deletions => Deletions;
    }
}
