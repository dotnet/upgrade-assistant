// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Common
{
    public static class AnalysisContextExtensions
    {
        public static void RegisterSimpleMemberAccessExpression(this AnalysisContext analysisContext, Action<SyntaxNodeAnalysisContext> analyze, Action<SyntaxNodeAnalysisContext>? vbAnalyze = null)
        {
            if (analysisContext is null)
            {
                return;
            }

            analysisContext.RegisterCompilationStartAction(compilationContext =>
            {
                if (compilationContext.Compilation.Language == LanguageNames.CSharp)
                {
                    compilationContext.RegisterSyntaxNodeAction(analyze, CS.SyntaxKind.SimpleMemberAccessExpression);
                }
                else if (compilationContext.Compilation.Language == LanguageNames.VisualBasic)
                {
                    compilationContext.RegisterSyntaxNodeAction(vbAnalyze ?? analyze, VB.SyntaxKind.SimpleMemberAccessExpression);
                }
            });
        }
    }
}
