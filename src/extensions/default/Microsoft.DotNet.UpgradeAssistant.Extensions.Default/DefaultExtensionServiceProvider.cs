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
        private const string TryConvertProjectConverterStepOptionsSection = "TryConvert";

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
            services.AddProjectFormatSteps()
                .Bind(services.Configuration.GetSection(TryConvertProjectConverterStepOptionsSection));
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
            services.AddTransient<DiagnosticAnalyzer, HttpContextCurrentAnalyzer>();

            // Code fix providers
            services.AddTransient<CodeFixProvider, HttpContextCurrentCodeFixer>();
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
