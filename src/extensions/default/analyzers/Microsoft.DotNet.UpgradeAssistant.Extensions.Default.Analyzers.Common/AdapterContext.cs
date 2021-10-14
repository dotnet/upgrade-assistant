// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

#pragma warning disable CA1062 // Validate arguments of public methods

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public record AdapterContext
    {
        public ImmutableArray<AdapterDescriptor> Descriptors { get; init; } = ImmutableArray.Create<AdapterDescriptor>();

        public ImmutableArray<FactoryDescriptor> Factories { get; init; } = ImmutableArray.Create<FactoryDescriptor>();

        public WellKnownTypes Types { get; init; }

        public bool IsFactoryMethod(IMethodSymbol method)
        {
            foreach (var factory in Factories)
            {
                foreach (var factoryMethod in factory.Methods)
                {
                    if (SymbolEqualityComparer.Default.Equals(method, factoryMethod))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public IMethodSymbol? GetFactory(ITypeSymbol type)
        {
            foreach (var factory in Factories)
            {
                foreach (var method in factory.Methods)
                {
                    if (SymbolEqualityComparer.Default.Equals(method.ReturnType, type))
                    {
                        return method;
                    }
                }
            }

            return null;
        }

        public AdapterDescriptor? GetDescriptorForDestination(ITypeSymbol type)
        {
            foreach (var descriptor in Descriptors)
            {
                if (SymbolEqualityComparer.Default.Equals(descriptor.Destination, type))
                {
                    return descriptor;
                }
            }

            return null;
        }

        public bool Ignore(ISymbol? symbol)
        {
            if (symbol is null)
            {
                return false;
            }

            if (symbol is IMethodSymbol methodSymbol)
            {
                foreach (var factory in Factories)
                {
                    foreach (var method in factory.Methods)
                    {
                        if (SymbolEqualityComparer.Default.Equals(method, methodSymbol))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool IsAvailable => Descriptors.Length > 0;

        public static AdapterContext Create() => new();

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
        public AdapterContext FromCompilation(Compilation compilation)
        {
            var context = this with
            {
                Types = WellKnownTypes.From(compilation)
            };

            context = context.FromAssembly(compilation.Assembly);

            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                {
                    context = context.FromAssembly(assembly);
                }
            }

            return context;
        }

        private AdapterContext FromAssembly(IAssemblySymbol assembly)
        {
            var context = this;

            foreach (var a in assembly.GetAttributes())
            {
                if (TryParseDescriptor(context.Types, a, out var descriptor))
                {
                    context = context with
                    {
                        Descriptors = context.Descriptors.Add(descriptor)
                    };
                }
                else if (TryParseFactory(context.Types, a, out var factory))
                {
                    context = context with
                    {
                        Factories = context.Factories.Add(factory)
                    };
                }
            }

            return context;
        }

        private static bool TryParseDescriptor(WellKnownTypes types, AttributeData a, [MaybeNullWhen(false)] out AdapterDescriptor descriptor)
        {
            descriptor = default;

            if (SymbolEqualityComparer.Default.Equals(types.AdapterDescriptor, a.AttributeClass))
            {
                if (a.ConstructorArguments.Length == 2 &&
                   a.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
                   a.ConstructorArguments[1].Kind == TypedConstantKind.Type &&
                   a.ConstructorArguments[0].Value is ITypeSymbol destination &&
                   a.ConstructorArguments[1].Value is ITypeSymbol concreteType)
                {
                    descriptor = new AdapterDescriptor(destination, concreteType);
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseFactory(WellKnownTypes types, AttributeData a, [MaybeNullWhen(false)] out FactoryDescriptor descriptor)
        {
            if (SymbolEqualityComparer.Default.Equals(types.DescriptorFactory, a.AttributeClass))
            {
                if (a.ConstructorArguments.Length == 2 &&
                    a.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
                    a.ConstructorArguments[1].Kind == TypedConstantKind.Primitive &&
                    a.ConstructorArguments[0].Value is ITypeSymbol type &&
                    a.ConstructorArguments[1].Value is string methodName)
                {
                    var methods = type.GetMembers(methodName)
                        .OfType<IMethodSymbol>()
                        .Where(m => m.IsStatic)
                        .ToImmutableArray();

                    if (methods.Length > 0)
                    {
                        descriptor = new FactoryDescriptor(methods);
                        return true;
                    }
                }
            }

            descriptor = default;
            return false;
        }
    }
}
