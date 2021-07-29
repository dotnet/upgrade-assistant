// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Dependencies
{
    public interface IDependencyCollection<T> : IEnumerable<T>
    {
        bool Add(T item, OperationDetails details);

        bool Remove(T item, OperationDetails details);

        [Obsolete("This API will be removed sometime after September 1, 2021. Please refactor to use a supported overload", error: false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool Add(T item, BuildBreakRisk risk = BuildBreakRisk.None);

        [Obsolete("This API will be removed sometime after September 1, 2021. Please refactor to use a supported overload", error: false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool Remove(T item, BuildBreakRisk risk = BuildBreakRisk.None);

        IReadOnlyCollection<Operation<T>> Additions { get; }

        IReadOnlyCollection<Operation<T>> Deletions { get; }
    }

    public record OperationDetails
    {
        public BuildBreakRisk Risk { get; init; } = BuildBreakRisk.None;

        public IEnumerable<string> Details { get; init; } = Enumerable.Empty<string>();
    }

    public record Operation<T>
    {
        public Operation(T item, OperationDetails details)
        {
            Item = item;
            OperationDetails = details;
        }

        public T Item { get; }

        public OperationDetails OperationDetails { get; }
    }
}
