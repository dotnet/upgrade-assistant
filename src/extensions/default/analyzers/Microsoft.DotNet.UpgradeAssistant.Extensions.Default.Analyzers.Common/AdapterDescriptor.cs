// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public record AdapterDescriptor(ITypeSymbol Destination, ITypeSymbol Original)
    {
        private const string ExpectedTypeKey = nameof(ExpectedTypeKey);

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
                    var value = Destination.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)

                        // Remove global identifier for C#
                        .Replace("global::", string.Empty)

                        // Remove global identifier for VB
                        .Replace("Global.", string.Empty);

                    Interlocked.Exchange(ref _properties, ImmutableDictionary.Create<string, string?>()
                        .Add(ExpectedTypeKey, value));
                }

                return _properties!;
            }
        }

        public static bool TryGetExpectedType(ImmutableDictionary<string, string?> dictionary, [MaybeNullWhen(false)] out string result)
            => dictionary.TryGetValue(ExpectedTypeKey, out result) && result is not null;

        /// <summary>
        /// Searches for instances of an adapter descriptor. This is an attribute that has the following shape.
        ///
        /// <![CDATA[
        /// public class AdapterDescriptor : Attribute
        /// {
        ///     public AdapterDescriptor(Type interfaceType, Type concreteType)
        ///     {
        ///     }
        /// } ]]>
        /// </summary>
        /// <param name="compilation">A <see cref="Compilation"/> instance.</param>
        /// <returns>A collection of adapter descriptors.</returns>
        public static ImmutableArray<AdapterDescriptor> Parse(Compilation compilation)
        {
            var result = ImmutableArray.CreateBuilder<AdapterDescriptor>();

            if (!Parse(result, compilation.Assembly))
            {
                return ImmutableArray.Create<AdapterDescriptor>();
            }

            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                {
                    Parse(result, assembly);
                }
            }

            return result.ToImmutable();
        }

        private static bool Parse(ImmutableArray<AdapterDescriptor>.Builder collection, IAssemblySymbol assembly)
        {
            foreach (var a in assembly.GetAttributes())
            {
                if (string.Equals(a.AttributeClass?.Name, "AdapterDescriptor", StringComparison.OrdinalIgnoreCase))
                {
                    if (a.ConstructorArguments.Length == 1 &&
                        a.ConstructorArguments[0].Kind == TypedConstantKind.Primitive &&
                        a.ConstructorArguments[0].Value is bool v &&
                        v)
                    {
                        return false;
                    }

                    if (a.ConstructorArguments.Length == 2 &&
                        a.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
                        a.ConstructorArguments[1].Kind == TypedConstantKind.Type &&
                        a.ConstructorArguments[0].Value is ITypeSymbol destination &&
                        a.ConstructorArguments[1].Value is ITypeSymbol concreteType)
                    {
                        collection.Add(new AdapterDescriptor(destination, concreteType));
                    }
                }
            }

            return true;
        }
    }
}
