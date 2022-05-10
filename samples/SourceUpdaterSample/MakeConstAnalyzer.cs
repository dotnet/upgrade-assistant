// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SourceUpdaterSample
{
    /// <summary>
    /// This sample analyzer (from Roslyn tutorials at https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)
    /// idenitifies variables that can be made constant. Upgrade Analyzer's source update step will use any types derived from
    /// DiagnosticAnalyzer that are registered in its dependency injection container along with an accompanying code fix provider.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MakeConstAnalyzer : DiagnosticAnalyzer
    {
        // The diagnostic ID can be any unique string. The default analyzers
        // that Upgrade Assistant uses have IDs prefixed with 'UA' for
        // 'Upgrade Assitant' but any diagnostic ID can be used.
        public const string DiagnosticId = "Sample1";

        // Upgrade Assistant analyzers typically have a category
        // of 'Upgrade' but, again, any value that makes sense for
        // the analyzer can be used here.
        private const string Category = "Upgrade";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.MakeConstTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.MakeConstMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.MakeConstDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            // Analyzers used by Upgrade Assistant can use any of the usual registered action
            // (syntax-based, symbol-based, operation-based, etc.) but bear in mind that source
            // updaters run relatively late in the upgrade process and changes may have been made
            // earlier (removing .NET Framework references, changing TFM, etc.) that could prevent
            // the project from building correctly. Because of that, analyzers that can work on
            // syntax are especially useful.
            context.RegisterSyntaxNodeAction(AnalyzeLocalDeclarationStatement, SyntaxKind.LocalDeclarationStatement);
        }

        // This analysis is specific to this sample analyzer, as described at
        // https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix
        private void AnalyzeLocalDeclarationStatement(SyntaxNodeAnalysisContext context)
        {
            var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;

            // Make sure the declaration isn't already const:
            if (localDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                return;
            }

            // Perform data flow analysis on the local declaration.
            var dataFlowAnalysis = context.SemanticModel.AnalyzeDataFlow(localDeclaration);

            if (dataFlowAnalysis is null)
            {
                return;
            }

            // Retrieve the local symbol for each variable in the local declaration
            // and ensure that it is not written outside of the data flow analysis region.
            var variable = localDeclaration.Declaration.Variables.Single();
            var variableSymbol = context.SemanticModel.GetDeclaredSymbol(variable);

            if (variableSymbol is null || dataFlowAnalysis.WrittenOutside.Contains(variableSymbol))
            {
                return;
            }

            var variableName = string.Join(", ", localDeclaration.Declaration.Variables.Select(v => v.Identifier.ValueText));
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), variableName));
        }
    }
}
