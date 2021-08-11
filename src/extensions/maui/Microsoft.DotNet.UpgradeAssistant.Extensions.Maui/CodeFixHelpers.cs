// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    public static partial class CodeFixHelpers
    {
        public static CompilationUnitSyntax AddUsingIfMissing(this CompilationUnitSyntax documentRoot, string namespaceName)
        {
            if (documentRoot is null)
            {
                throw new ArgumentNullException(nameof(documentRoot));
            }

            if (string.IsNullOrEmpty(namespaceName))
            {
                throw new ArgumentException($"'{nameof(namespaceName)}' cannot be null or empty.", nameof(namespaceName));
            }

            if (documentRoot.Usings.Any(u => u.Name.ToString().Equals(namespaceName, StringComparison.Ordinal)))
            {
                return documentRoot;
            }

            var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName).WithLeadingTrivia(SyntaxFactory.Whitespace(" ")))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
            return documentRoot.AddUsings(usingDirective);
        }
    }
}
