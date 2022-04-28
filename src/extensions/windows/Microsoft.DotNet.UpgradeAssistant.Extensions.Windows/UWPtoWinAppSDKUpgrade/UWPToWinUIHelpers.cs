﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade
{
    internal class UWPToWinUIHelpers
    {
        public static IEnumerable<string> GetAllImportedNamespaces(SyntaxNodeAnalysisContext context)
        {
            return context.Node.Ancestors().OfType<CompilationUnitSyntax>().First().DescendantNodes().OfType<UsingDirectiveSyntax>()
                .Select(usingDirective => usingDirective.Name.ToString());
        }
    }
}
