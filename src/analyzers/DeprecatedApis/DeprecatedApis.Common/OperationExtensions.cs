// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer
{
    public static class OperationExtensions
    {
        public static bool IsVisualBasic(this IOperation operation) => operation?.Language == LanguageNames.VisualBasic;

        public static bool IsCSharp(this IOperation operation) => operation?.Language == LanguageNames.CSharp;

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

        public static IOperation? GetEnclosingMethodOperation(this IOperation? operation)
            => operation.GetParentOperationWhere(static o => o.IsEnclosedMethodOperation());

        public static IMethodSymbol? GetEnclosingMethod(this IOperation? operation)
            => operation.GetEnclosingMethodOperation() is IOperation enclosed && enclosed.SemanticModel.GetDeclaredSymbol(enclosed.Syntax) is IMethodSymbol method
                ? method
                : null;

        public static bool IsEnclosedMethodOperation(this IOperation operation)
        {
            if (operation.IsVisualBasic())
            {
                return operation is IBlockOperation && operation.Syntax is VBSyntax.MethodBlockSyntax;
            }
            else if (operation.IsCSharp())
            {
                return operation is IMethodBodyOperation;
            }

            throw new NotImplementedException(Resources.UnknownLanguage);
        }
    }
}
