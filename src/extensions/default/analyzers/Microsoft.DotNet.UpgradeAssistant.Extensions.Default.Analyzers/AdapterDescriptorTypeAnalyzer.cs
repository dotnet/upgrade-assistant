// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable CA1062 // Validate arguments of public methods

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class AdapterDescriptorTypeAnalyzer : DiagnosticAnalyzer
    {
        public const string AttributeDiagnosticId = "UA0014d";
        public const string ConstructorCountDiagnosticId = "UA0014e";
        public const string ParameterDiagnosticId = "UA0014f";
        public const string ParameterCountDiagnosticId = "UA0014g";
        public const string AddAdapterDescriptorDiagnosticId = "UA0014l";

        private const string Category = "Refactor";

        private static readonly LocalizableString AdapterDescriptorTypeAttributeTitle = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeAttributeTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeAttributeMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeAttributeMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeAttributeDescription = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeAttributeDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString AdapterDescriptorTypeConstructorCountTitle = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeConstructorCountTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeConstructorCountMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeConstructorCountMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeConstructorCountDescription = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeConstructorCountDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString AdapterDescriptorTypeParameterTitle = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeParameterTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeParameterMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeParameterMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeParameterDescription = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeParameterDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString AdapterDescriptorTypeParameterCountTitle = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeParameterCountTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeParameterCountMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeParameterCountMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeParameterCountDescription = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeParameterCountDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString AddAdapterDescriptorTitle = new LocalizableResourceString(nameof(Resources.AddAdapterDescriptorTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AddAdapterDescriptorMessageFormat = new LocalizableResourceString(nameof(Resources.AddAdapterDescriptorMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AddAdapterDescriptorDescription = new LocalizableResourceString(nameof(Resources.AddAdapterDescriptorDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor AttributeRule = new(AttributeDiagnosticId, AdapterDescriptorTypeAttributeTitle, AdapterDescriptorTypeAttributeMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: AdapterDescriptorTypeAttributeDescription);
        private static readonly DiagnosticDescriptor ConstructorCountRule = new(ConstructorCountDiagnosticId, AdapterDescriptorTypeConstructorCountTitle, AdapterDescriptorTypeConstructorCountMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: AdapterDescriptorTypeConstructorCountDescription);
        private static readonly DiagnosticDescriptor ParameterRule = new(ParameterDiagnosticId, AdapterDescriptorTypeParameterTitle, AdapterDescriptorTypeParameterMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: AdapterDescriptorTypeParameterDescription);
        private static readonly DiagnosticDescriptor ParameterCountRule = new(ParameterCountDiagnosticId, AdapterDescriptorTypeParameterCountTitle, AdapterDescriptorTypeParameterCountMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: AdapterDescriptorTypeParameterCountDescription);
        private static readonly DiagnosticDescriptor AddAdapterDescriptorRule = new(AddAdapterDescriptorDiagnosticId, AddAdapterDescriptorTitle, AddAdapterDescriptorMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: AddAdapterDescriptorDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(AttributeRule, ConstructorCountRule, ParameterRule, ParameterCountRule, AddAdapterDescriptorRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSymbolAction(context =>
            {
                var types = WellKnownTypes.From(context.Symbol);

                if (types.IsEmpty)
                {
                    return;
                }

                var systemType = context.Compilation.GetTypeByMetadataName("System.Type");
                var systemString = context.Compilation.GetTypeByMetadataName("System.String");

                if (systemType is null || systemString is null)
                {
                    return;
                }

                ValidateDescriptor(context, types.AdapterDescriptor, systemType, systemType);
                ValidateDescriptor(context, types.DescriptorFactory, systemType, systemString);
                ValidateDescriptor(context, types.AdapterStaticDescriptor, systemType, systemString, systemType, systemString);
            }, SymbolKind.NamedType);

            context.RegisterCompilationStartAction(context =>
            {
                var adapterContext = AdapterContext.Create().FromCompilation(context.Compilation);

                if (adapterContext.Types.AdapterDescriptor is null)
                {
                    RegisterAddAdapterDescriptorActions(context);
                    return;
                }
            });
        }

        private static void ValidateAttribute(SymbolAnalysisContext context, INamedTypeSymbol type)
        {
            var attribute = context.Compilation.GetTypeByMetadataName("System.Attribute");

            if (!SymbolEqualityComparer.Default.Equals(type.BaseType, attribute))
            {
                foreach (var location in type.Locations)
                {
                    context.ReportDiagnostic(Diagnostic.Create(AttributeRule, location, type.ToDisplayString()));
                }
            }
        }

        private static void ValidateDescriptor(SymbolAnalysisContext context, INamedTypeSymbol? type, params INamedTypeSymbol[] parameterTypes)
        {
            if (type is null)
            {
                return;
            }

            ValidateAttribute(context, type);

            if (type.InstanceConstructors.Length > 2)
            {
                foreach (var location in type.Locations)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ConstructorCountRule, location, type.ToDisplayString(), 2));
                }
            }

            var constructor = type.InstanceConstructors[0];

            if (constructor.Parameters.Length != parameterTypes.Length)
            {
                foreach (var location in constructor.Locations)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ParameterCountRule, location, type.ToDisplayString(), 2));
                }
            }

            for (var i = 0; i < Math.Min(constructor.Parameters.Length, parameterTypes.Length); i++)
            {
                var p = constructor.Parameters[i];
                var expectedType = parameterTypes[i];

                if (!SymbolEqualityComparer.Default.Equals(expectedType, p.Type))
                {
                    foreach (var location in p.Locations)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ParameterRule, location, type.ToDisplayString(), expectedType.ToDisplayString()));
                    }
                }
            }
        }

        private static void RegisterAddAdapterDescriptorActions(CompilationStartAnalysisContext context)
        {
            if (context.Compilation.Language != LanguageNames.CSharp)
            {
                return;
            }

            var deprecatedTypeSymbols = InitializeDeprecatedTypeSymbols(context.Compilation);

            context.RegisterSyntaxNodeAction(context =>
            {
                if (context.Node is CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax method)
                {
                    var symbol = context.SemanticModel.GetDeclaredSymbol(method);
                    GeneralizedSyntaxNodeAction(context, deprecatedTypeSymbols, method.ReturnType);
                    GeneralizedParameterAction(context, deprecatedTypeSymbols, method.ParameterList.Parameters, static n => n.Type);
                }
                else if (context.Node is CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax field)
                {
                    GeneralizedSyntaxNodeAction(context, deprecatedTypeSymbols, field.Declaration.Type);
                }
                else if (context.Node is CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax property)
                {
                    GeneralizedSyntaxNodeAction(context, deprecatedTypeSymbols, property.Type);
                }
            }, CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration, CodeAnalysis.CSharp.SyntaxKind.PropertyDeclaration, CodeAnalysis.CSharp.SyntaxKind.FieldDeclaration);
        }

        private static HashSet<ISymbol> InitializeDeprecatedTypeSymbols(Compilation compilation)
        {
            var deprecatedTypeNames = new[]
            {
                "System.Web.HttpContext",
                "System.Web.HttpContextBase",
                "Microsoft.AspNetCore.Http.HttpRequest",
                "Microsoft.AspNetCore.Http.HttpResponse",
            };

#pragma warning disable RS1024 // Compare symbols correctly (Known false positive: https://github.com/dotnet/roslyn-analyzers/issues/4568)
            var deprecatedTypeSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly

            foreach (var name in deprecatedTypeNames)
            {
                var typeSymbol = compilation.GetTypeByMetadataName(name);
                if (typeSymbol is not null)
                {
                    deprecatedTypeSymbols.Add(typeSymbol);
                }
            }

            return deprecatedTypeSymbols;
        }

        private static void GeneralizedSyntaxNodeAction(
            SyntaxNodeAnalysisContext context,
            HashSet<ISymbol> deprecatedTypeSymbols,
            SyntaxNode? syntaxNode)
        {
            if (syntaxNode is null)
            {
                return;
            }

            var symbol = context.SemanticModel.GetSymbolInfo(syntaxNode).Symbol;

            if (symbol is null)
            {
                return;
            }

            if (deprecatedTypeSymbols.Contains(symbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(AddAdapterDescriptorRule, syntaxNode.GetLocation(), syntaxNode.ToFullString().Trim()));
            }
        }

        private static void GeneralizedParameterAction<TParameter>(
            SyntaxNodeAnalysisContext context,
            HashSet<ISymbol> deprecatedTypeSymbols,
            SeparatedSyntaxList<TParameter> parameters,
            Func<TParameter, SyntaxNode?> parameterToType)
            where TParameter : SyntaxNode
        {
            var method = context.SemanticModel.GetDeclaredSymbol(context.Node);

            if (method is null)
            {
                return;
            }

            foreach (var p in parameters)
            {
                if (parameterToType(p) is SyntaxNode type && context.SemanticModel.GetSymbolInfo(type) is SymbolInfo info && info.Symbol is ISymbol parameterSymbol)
                {
                    if (deprecatedTypeSymbols.Contains(parameterSymbol))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(AddAdapterDescriptorRule, type.GetLocation(), parameterSymbol.Name));
                    }
                }
            }
        }
    }
}
