// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#pragma warning disable CA1062 // Validate arguments of public methods

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class AdapterFactoryDescriptorUsageAnalyzer : DiagnosticAnalyzer
    {
        public const string MustExistDiagnosticId = "UA0014h";
        public const string MustBeStaticDiagnosticId = "UA0014i";

        private const string Category = "Refactor";

        private static readonly LocalizableString MustExistTitle = new LocalizableResourceString(nameof(Resources.AdapterFactoryMustExistTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MustExistMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterFactoryMustExistMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MustExistDescription = new LocalizableResourceString(nameof(Resources.AdapterFactoryMustExistDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString MustBeStaticTitle = new LocalizableResourceString(nameof(Resources.AdapterFactoryMustBeStaticTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MustBeStaticMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterFactoryMustBeStaticMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MustBeStaticDescription = new LocalizableResourceString(nameof(Resources.AdapterFactoryMustBeStaticDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor MustExistRule = new(MustExistDiagnosticId, MustExistTitle, MustExistMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: MustExistDescription);
        private static readonly DiagnosticDescriptor MustBeStaticRule = new(MustBeStaticDiagnosticId, MustBeStaticTitle, MustBeStaticMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: MustBeStaticDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(MustExistRule, MustBeStaticRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterOperationAction(context =>
            {
                var symbol = context.Operation.SemanticModel.GetSymbolInfo(context.Operation.Syntax);

                var types = WellKnownTypes.From(symbol.Symbol);

                if (types?.DescriptorFactory is null)
                {
                    return;
                }

                var children = context.Operation.Children.ToImmutableArray();

                if (children.Length != 2)
                {
                    return;
                }

                if (children[0] is ITypeOfOperation typeOf && typeOf.TypeOperand is INamedTypeSymbol type &&
                    children[1] is ILiteralOperation literal && literal.ConstantValue.HasValue && literal.ConstantValue.Value is string methodName)
                {
                    var methods = type.GetMembers(methodName);

                    if (methods.Length == 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(MustExistRule, literal.Syntax.GetLocation(), methodName, type.ToDisplayString()));
                    }

                    foreach (var method in methods)
                    {
                        if (!method.IsStatic)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(MustBeStaticRule, literal.Syntax.GetLocation(), method.ToDisplayString()));
                        }
                    }
                }
            }, OperationKind.None);
        }
    }
}
