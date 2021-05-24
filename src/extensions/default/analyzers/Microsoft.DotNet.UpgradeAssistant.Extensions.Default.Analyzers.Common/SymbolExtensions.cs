// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Common;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default
{
    public static class SymbolExtensions
    {
        public static bool NameEquals(this ISymbol? symbol, string name)
            => string.Equals(symbol?.Name, name, symbol.GetStringComparison());

        public static StringComparison GetStringComparison(this ISymbol? symbol)
            => symbol?.Language switch
            {
                LanguageNames.CSharp => StringComparison.Ordinal,
                LanguageNames.VisualBasic => StringComparison.OrdinalIgnoreCase,
                _ => throw new NotImplementedException(Resources.UnknownLanguage),
            };

        public static StringComparer GetStringComparer(this ISymbol? symbol)
            => symbol?.Language switch
            {
                LanguageNames.CSharp => StringComparer.Ordinal,
                LanguageNames.VisualBasic => StringComparer.OrdinalIgnoreCase,
                _ => throw new NotImplementedException(Resources.UnknownLanguage),
            };

        /// <summary>
        /// Finds all members including those on the base classes.
        /// </summary>
        /// <param name="symbol">Symbol to search</param>
        /// <returns>A collection of base members.</returns>
        public static IEnumerable<ISymbol> GetAllMembers(this INamedTypeSymbol? symbol)
        {
            var allowPrivate = true;

            while (symbol is not null)
            {
                foreach (var member in symbol.GetMembers())
                {
                    if (member.IsImplicitlyDeclared)
                    {
                        continue;
                    }

                    if (!allowPrivate && member.DeclaredAccessibility == Accessibility.Private)
                    {
                        continue;
                    }

                    yield return member;
                }

                // After the first level, we cannot see private
                allowPrivate = false;

                symbol = symbol.BaseType;
            }
        }

        public static bool NameEquals(this IAssemblySymbol? symbol, string name, bool startsWith = true)
        {
            if (symbol is null)
            {
                return false;
            }

            // This compares the assembly name which is case-insensitive for all languages.
            if (startsWith)
            {
                return symbol.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return string.Equals(symbol.Name, name, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets the first existing parameter of the type from node if it is a method.
        /// </summary>
        /// <param name="node">A node that is potentially a method.</param>
        /// <param name="semanticModel">The current semantic model.</param>
        /// <param name="type">The type that should be matched.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A parameter symbol if a match is found.</returns>
        public static IParameterSymbol? GetExistingParameterSymbolByType(this SyntaxNode node, SemanticModel semanticModel, ITypeSymbol? type, CancellationToken token)
        {
            if (type is null)
            {
                return null;
            }

            var symbol = semanticModel.GetDeclaredSymbol(node, token);

            if (symbol is not IMethodSymbol method)
            {
                return null;
            }

            return method.Parameters.FirstOrDefault(p =>
            {
                if (p.Type is null)
                {
                    return false;
                }

                return SymbolEqualityComparer.IncludeNullability.Equals(p.Type, type);
            });
        }

        /// <summary>
        /// Gets a parent operation that satisfies the given predicate.
        /// </summary>
        /// <param name="operation">A starting operation.</param>
        /// <param name="func">A predicate to check matches against.</param>
        /// <returns>A matching operation if found.</returns>
        public static IOperation? GetParentOperationWhere(this IOperation? operation, Func<IOperation, bool> func)
        {
            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            while (operation is not null)
            {
                if (func(operation))
                {
                    return operation;
                }

                operation = operation.Parent;
            }

            return default;
        }
    }
}
