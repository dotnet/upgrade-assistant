// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.ConfigUpdaters;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default
{
    public class DefaultExtensionServiceProvider : IExtensionServiceProvider
    {
        private const string TryConvertProjectConverterStepOptionsSection = "TryConvertProjectConverter";

        public void AddServices(IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new System.ArgumentNullException(nameof(services));
            }

            AddUpgradeSteps(services, services.Configuration);
            AddConfigUpdaters(services.Services);
            AddAnalyzersAndCodeFixProviders(services.Services);
            AddPackageReferenceAnalyzers(services.Services);
        }

        private static void AddUpgradeSteps(IExtensionServiceCollection services, IConfiguration configuration)
        {
            services.Services.AddBackupStep();
            services.AddConfigUpdaterStep();
            services.AddPackageUpdaterStep();
            services.Services.AddProjectFormatSteps()
                .Bind(configuration.GetSection(TryConvertProjectConverterStepOptionsSection));
            services.Services.AddSolutionSteps();
            services.Services.AddSourceUpdaterStep();
            services.AddTemplateInserterStep();
            services.Services.AddRazorUpdaterStep();
        }

        // This extension only adds default config updaters, but other extensions
        // can register additional updaters, as needed.
        private static void AddConfigUpdaters(IServiceCollection services)
        {
            services.AddScoped<IUpdater<ConfigFile>, AppSettingsConfigUpdater>();
            services.AddScoped<IUpdater<ConfigFile>, UnsupportedSectionConfigUpdater>();
            services.AddScoped<IUpdater<ConfigFile>, WebNamespaceConfigUpdater>();
        }

        // This extension only adds default analyzers and code fix providers, but other extensions
        // can register additional analyzers and code fix providers, as needed.
        private static void AddAnalyzersAndCodeFixProviders(IServiceCollection services)
        {
            // Add source analyzers and code fix providers (note that order doesn't matter as they're run alphabetically)
            // Analyzers
            services.AddTransient<DiagnosticAnalyzer, AllowHtmlAttributeAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, BinaryFormatterUnsafeDeserializeAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, HtmlHelperAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, HttpContextCurrentAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, HttpContextIsDebuggingEnabledAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, TypeUpgradeAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, UrlHelperAnalyzer>();
            services.AddTransient<DiagnosticAnalyzer, UsingSystemWebAnalyzer>();

            // Code fix providers
            services.AddTransient<CodeFixProvider, AllowHtmlAttributeCodeFixer>();
            services.AddTransient<CodeFixProvider, BinaryFormatterUnsafeDeserializeCodeFixer>();
            services.AddTransient<CodeFixProvider, HtmlHelperCodeFixer>();
            services.AddTransient<CodeFixProvider, HttpContextCurrentCodeFixer>();
            services.AddTransient<CodeFixProvider, HttpContextIsDebuggingEnabledCodeFixer>();
            services.AddTransient<CodeFixProvider, TypeUpgradeCodeFixer>();
            services.AddTransient<CodeFixProvider, UrlHelperCodeFixer>();
            services.AddTransient<CodeFixProvider, UsingSystemWebCodeFixer>();

            // Add additional documents used by the analzyers
            services.AddTransient<AdditionalText, WebTypeReplacements>();
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
            services.AddTransient<IDependencyAnalyzer, NewtonsoftReferenceAnalyzer>();
        }
    }
}
