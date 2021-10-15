// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public record AdapterDescriptor<TSymbol>(TSymbol Original, TSymbol Destination)
        where TSymbol : ISymbol
    {
        private string? _originalMessage;
        private string? _destinationMessage;
        private ImmutableDictionary<string, string?>? _properties;

        public string OriginalMessage
        {
            get
            {
                if (_originalMessage is null)
                {
                    Interlocked.Exchange(ref _originalMessage, Original.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat));
                }

                return _originalMessage!;
            }
        }

        public string DestinationMessage
        {
            get
            {
                if (_destinationMessage is null)
                {
                    Interlocked.Exchange(ref _destinationMessage, Destination.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat));
                }

                return _destinationMessage!;
            }
        }

        public ImmutableDictionary<string, string?> Properties
        {
            get
            {
                if (_properties is null)
                {
                    Interlocked.Exchange(ref _properties, ImmutableDictionary.Create<string, string?>()
                        .WithSymbol(Destination));
                }

                return _properties!;
            }
        }
    }
}
