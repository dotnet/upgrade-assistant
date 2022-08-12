// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade.Utils
{
    internal static class SyntaxNodeExtensions
    {
        public static IEnumerable<string> GetAllImportedNamespaces(this SyntaxNode node)
        {
            var root = node.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();
            if (root is null)
            {
                return Enumerable.Empty<string>();
            }

            return root.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(usingDirective => usingDirective.Name.ToString());
        }
    }
}
