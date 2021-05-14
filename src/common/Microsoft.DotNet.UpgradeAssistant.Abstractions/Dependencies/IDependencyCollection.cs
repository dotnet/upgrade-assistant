// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Dependencies
{
    public interface IDependencyCollection<T> : IEnumerable<T>
    {
        bool Add(T item, BuildBreakRisk risk = BuildBreakRisk.None);

        bool Remove(T item, BuildBreakRisk risk = BuildBreakRisk.None);

        IReadOnlyCollection<T> Additions { get; }

        IReadOnlyCollection<T> Deletions { get; }
    }
}
