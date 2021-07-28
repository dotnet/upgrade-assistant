﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Dependencies
{
    public interface IDependencyCollection<T> : IEnumerable<T>
    {
        bool Add(T item, OperationDetails details);

        bool Remove(T item, OperationDetails details);

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
            Action = details;
        }

        public T Item { get; }

        public OperationDetails Action { get; }
    }
}
