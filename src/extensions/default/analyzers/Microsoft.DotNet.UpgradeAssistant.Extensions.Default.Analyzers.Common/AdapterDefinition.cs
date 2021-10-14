// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public record AdapterDefinition(ITypeSymbol TypeToReplace)
    {
        private ImmutableDictionary<string, string?>? _properties;

        public ImmutableDictionary<string, string?> Properties
        {
            get
            {
                if (_properties is null)
                {
                    Interlocked.Exchange(ref _properties,
                        ImmutableDictionary
                            .Create<string, string?>()
                            .WithTypeToReplace(TypeToReplace));
                }

                return _properties!;
            }
        }
    }
}
