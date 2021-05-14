// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class DependencyCollection<T> : IDependencyCollection<T>
    {
        private readonly IEnumerable<T> _initial;
        private readonly Action<BuildBreakRisk> _setRisk;

        public DependencyCollection(IEnumerable<T> initial, Action<BuildBreakRisk> setRisk)
        {
            _initial = initial;
            _setRisk = setRisk;
        }

        public HashSet<T> Additions { get; } = new HashSet<T>();

        public HashSet<T> Deletions { get; } = new HashSet<T>();

        bool IDependencyCollection<T>.Add(T item, BuildBreakRisk risk)
        {
            if (_initial.Contains(item))
            {
                return false;
            }

            if (Additions.Add(item))
            {
                _setRisk(risk);
                return true;
            }

            return false;
        }

        bool IDependencyCollection<T>.Remove(T item, BuildBreakRisk risk)
        {
            if (!_initial.Contains(item))
            {
                return false;
            }

            if (Deletions.Add(item))
            {
                _setRisk(risk);
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
                    yield return item;
                }
            }

            foreach (var additions in Additions)
            {
                yield return additions;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool HasChanges => Additions.Any() || Deletions.Any();

        IReadOnlyCollection<T> IDependencyCollection<T>.Additions => Additions;

        IReadOnlyCollection<T> IDependencyCollection<T>.Deletions => Deletions;
    }
}
