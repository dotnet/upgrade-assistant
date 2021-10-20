// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer
{
    public static class AdapterContextExtensions
    {
        /// <summary>
        /// Searches for instances of an adapter descriptor. This is an attribute that has the following shape.
        ///
        /// <![CDATA[
        /// public class AdapterDescriptor : Attribute
        /// {
        ///     public AdapterDescriptor(Type concreteType, Type? interfaceType = null)
        ///     {
        ///     }
        /// } ]]>
        /// </summary>
        /// <param name="compilation">A <see cref="Compilation"/> instance.</param>
        /// <returns>A collection of adapter descriptors.</returns>
        public static AdapterContext FromCompilation(this AdapterContext context, Compilation compilation)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (compilation is null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            context = context with
            {
                KnownTypes = WellKnownTypes.From(compilation)
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

        private static AdapterContext FromAssembly(this AdapterContext context, IAssemblySymbol assembly)
        {
            foreach (var a in assembly.GetAttributes())
            {
                ParseDescriptor(a, ref context);
                ParseStaticDescriptor(a, ref context);
                ParseFactories(a, ref context);
            }

            return context;
        }

        private static void ParseStaticDescriptor(AttributeData a, ref AdapterContext context)
        {
            if (SymbolEqualityComparer.Default.Equals(context.KnownTypes.AdapterStaticDescriptor, a.AttributeClass))
            {
                if (a.ConstructorArguments.Length == 4 &&
                   a.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
                   a.ConstructorArguments[1].Kind == TypedConstantKind.Primitive &&
                   a.ConstructorArguments[2].Kind == TypedConstantKind.Type &&
                   a.ConstructorArguments[3].Kind == TypedConstantKind.Primitive &&
                   a.ConstructorArguments[0].Value is ITypeSymbol concreteType &&
                   a.ConstructorArguments[1].Value is string concreteMember &&
                   a.ConstructorArguments[2].Value is ITypeSymbol destinationType &&
                   a.ConstructorArguments[3].Value is string destinationMember)
                {
                    if (destinationType.GetMembers(destinationMember).FirstOrDefault() is ISymbol destinationSymbol &&
                        concreteType.GetMembers(concreteMember).FirstOrDefault() is ISymbol concreteSymbol)
                    {
                        context = context with { StaticMembers = context.StaticMembers.Add(new(concreteSymbol, destinationSymbol)) };
                    }
                }
            }
        }

        private static void ParseDescriptor(AttributeData a, ref AdapterContext context)
        {
            if (SymbolEqualityComparer.Default.Equals(context.KnownTypes.AdapterDescriptor, a.AttributeClass))
            {
                if (a.ConstructorArguments.Length == 2 &&
                   a.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
                   a.ConstructorArguments[1].Kind == TypedConstantKind.Type &&
                   a.ConstructorArguments[0].Value is ITypeSymbol concreteType)
                {
                    if (a.ConstructorArguments[1].Value is ITypeSymbol destination)
                    {
                        var descriptor = new ReplacementDescriptor<ITypeSymbol>(concreteType, destination);
                        context = context with { Types = context.Types.Add(descriptor) };
                    }
                    else
                    {
                        var definition = new ApiDescriptor(concreteType);
                        context = context with { Apis = context.Apis.Add(definition) };
                    }
                }
            }
        }

        private static void ParseFactories(AttributeData a, ref AdapterContext context)
        {
            if (SymbolEqualityComparer.Default.Equals(context.KnownTypes.DescriptorFactory, a.AttributeClass))
            {
                if (a.ConstructorArguments.Length == 2 &&
                    a.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
                    a.ConstructorArguments[1].Kind == TypedConstantKind.Primitive &&
                    a.ConstructorArguments[0].Value is ITypeSymbol type &&
                    a.ConstructorArguments[1].Value is string methodName)
                {
                    var members = type.GetMembers(methodName);

                    if (members.Length > 0)
                    {
                        var builder = context.Factories.ToBuilder();

                        foreach (var member in members)
                        {
                            if (member is IMethodSymbol method && method.IsStatic)
                            {
                                builder.Add(new(method));
                            }
                        }

                        context = context with { Factories = builder.ToImmutable() };
                    }
                }
            }
        }
    }
}
