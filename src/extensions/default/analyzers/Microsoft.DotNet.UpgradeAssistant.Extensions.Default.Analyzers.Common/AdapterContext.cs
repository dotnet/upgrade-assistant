// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public record AdapterContext
    {
        public ImmutableArray<AdapterDescriptor> Descriptors { get; init; } = ImmutableArray.Create<AdapterDescriptor>();

        public bool IsAvailable => Descriptors.Length > 0;

        public static AdapterContext Parse(Compilation compilation)
            => new()
            {
                Descriptors = AdapterDescriptor.Parse(compilation)
            };
    }
}
