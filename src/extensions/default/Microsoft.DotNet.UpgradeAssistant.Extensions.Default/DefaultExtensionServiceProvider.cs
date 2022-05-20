// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default
{
    public class DefaultExtensionServiceProvider : IExtensionServiceProvider
    {
        public void AddServices(IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            AddUpgradeSteps(services);
            AddAnalyzersAndCodeFixProviders(services.Services);
            AddPackageReferenceAnalyzers(services.Services);
        }

        private static void AddUpgradeSteps(IExtensionServiceCollection services)
        {
            services.AddBackupStep();
            services.AddConfigUpdaterStep();
            services.AddPackageUpdaterStep();
            services.AddSolutionSteps();
            services.AddSourceUpdaterStep();
            services.AddTemplateInserterStep();
        }

        // This extension only adds default analyzers and code fix providers, but other extensions
        // can register additional analyzers and code fix providers, as needed.
        private static void AddAnalyzersAndCodeFixProviders(IServiceCollection services)
        {
            // Add source analyzers and code fix providers (note that order doesn't matter as they're run alphabetically)
            // Analyzers
            services.AddTransient<DiagnosticAnalyzer, ApiAlertAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, WinUIApiAlertAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, AttributeUpgradeAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, BinaryFormatterUnsafeDeserializeAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, HtmlHelperAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, HttpContextCurrentAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, HttpContextIsDebuggingEnabledAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, TypeUpgradeAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, UrlHelperAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, UsingSystemWebAnalyzer>();

            // Code fix providers
            services.AddTransient<CodeFixProvider, AttributeUpgradeCodeFixer>();
            services.AddTransient<CodeFixProvider, BinaryFormatterUnsafeDeserializeCodeFixer>();
            services.AddTransient<CodeFixProvider, HtmlHelperCodeFixer>();
            services.AddTransient<CodeFixProvider, HttpContextCurrentCodeFixer>();
            services.AddTransient<CodeFixProvider, HttpContextIsDebuggingEnabledCodeFixer>();
            services.AddTransient<CodeFixProvider, TypeUpgradeCodeFixer>();
            services.AddTransient<CodeFixProvider, UrlHelperCodeFixer>();
            services.AddTransient<CodeFixProvider, UsingSystemWebCodeFixer>();
        }

        // This extension only adds default package reference analyzers, but other extensions
        // can register additional package reference analyzers, as needed.
        private static void AddPackageReferenceAnalyzers(IServiceCollection services)
        {
            // Add package analyzers (note that the order matters as the analyzers are run in the order registered)
            services.AddTransient<IDependencyAnalyzer, DuplicateReferenceAnalyzer>();
            services.AddTransient<IDependencyAnalyzer, TransitiveReferenceAnalyzer>();
            services.AddTransient<IDependencyAnalyzer, PackageMapReferenceAnalyzer>();
            services.AddTransient<IDependencyAnalyzer, TargetCompatibilityReferenceAnalyzer>();
            services.AddTransient<IDependencyAnalyzer, UpgradeAssistantReferenceAnalyzer>();
            services.AddTransient<IDependencyAnalyzer, WindowsCompatReferenceAnalyzer>();
        }
    }
}
