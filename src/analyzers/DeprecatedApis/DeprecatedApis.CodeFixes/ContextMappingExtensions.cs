// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.CodeFixes
{
    internal static class ContextMappingExtensions
    {
        public static ITypeSymbol MapType(this AdapterContext context, ITypeSymbol type)
        {
            foreach (var descriptor in context.TypeDescriptors)
            {
                if (SymbolEqualityComparer.Default.Equals(descriptor.Original, type))
                {
                    return descriptor.Destination;
                }
            }

            return type;
        }

        public static SyntaxNode Declaration(this SyntaxGenerator generator, ISymbol symbol, AdapterContext context)
            => symbol switch
            {
                IPropertySymbol property => generator.PropertyDeclaration(property, context),
                IMethodSymbol method => generator.Declaration(method, context),
                _ => throw new NotImplementedException(),
            };

        public static SyntaxNode PropertyDeclaration(this SyntaxGenerator generator, IPropertySymbol property, AdapterContext context)
            => generator.PropertyDeclaration(
                property.Name,
                generator.TypeExpression(context.MapType(property.Type)),
                property.DeclaredAccessibility,
                DeclarationModifiers.From(property));

        public static SyntaxNode Declaration(this SyntaxGenerator generator, IMethodSymbol method, AdapterContext context)
            => generator.MethodDeclaration(
                           method.Name,
                           parameters: method.Parameters.Select(p => generator.ParameterDeclaration(p, context)),
                           returnType: method.ReturnType.IsSystemVoid() ? null : generator.TypeExpression(context.MapType(method.ReturnType)),
                           accessibility: method.DeclaredAccessibility,
                           modifiers: DeclarationModifiers.From(method));

        public static SyntaxNode ParameterDeclaration(this SyntaxGenerator generator, IParameterSymbol symbol, AdapterContext context)
             => generator.ParameterDeclaration(
                    symbol.Name,
                    generator.TypeExpression(context.MapType(symbol.Type)),
                    initializer: null,
                    symbol.RefKind);

        public static bool IsSystemVoid([NotNullWhen(returnValue: true)] this ITypeSymbol? symbol)
            => symbol?.SpecialType == SpecialType.System_Void;
    }
}
