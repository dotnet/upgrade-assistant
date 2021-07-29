// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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

        public bool Add(T item, OperationDetails od)
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

        public bool Remove(T item, OperationDetails od)
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

        [Obsolete("This API will be removed sometime after September 1, 2021. Please refactor to use a supported overload", error: false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool IDependencyCollection<T>.Add(T item, BuildBreakRisk risk)
        {
            return Add(item, new() { Risk = risk });
        }

        [Obsolete("This API will be removed sometime after September 1, 2021. Please refactor to use a supported overload", error: false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool IDependencyCollection<T>.Remove(T item, BuildBreakRisk risk)
        {
            return Remove(item, new() { Risk = risk });
        }

        public bool HasChanges => Additions.Any() || Deletions.Any();

        IReadOnlyCollection<Operation<T>> IDependencyCollection<T>.Additions => Additions;

        IReadOnlyCollection<Operation<T>> IDependencyCollection<T>.Deletions => Deletions;
    }
}
