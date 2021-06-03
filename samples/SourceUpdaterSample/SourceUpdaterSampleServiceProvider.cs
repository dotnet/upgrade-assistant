// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace SourceUpdaterSample
{
    /// <summary>
    /// Extension authors can implement the IExtensionServiceProvider interface to
    /// register services with Upgrade Assistant's dependency injection container.
    /// This could include registering additional upgrade steps. It might also include
    /// registering services needed by the steps registered or other migrations steps.
    /// For example, registering Roslyn analyzer/code fix providers, IConfigUpdaters,
    /// or IPackageReferenceAnalyzers will cause upgrade steps that use those types to
    /// pick the newly registered services up automatically and use them.
    /// </summary>
    public class SourceUpdaterSampleServiceProvider : IExtensionServiceProvider
    {
        /// <summary>
        /// Registers services (the analyzer and code fix provider) comprising the
        /// SourceUpdaterSample extension into Upgrade Assistant's dependency injection container.
        /// </summary>
        /// <param name="services">A configuration object containing the service collection
        /// to register services in, the extension's configuration file, and a file provider
        /// for retrieving extension files.</param>
        public void AddServices(IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register the analyzer and code fix provider for this extension.
            // Even though this extension doesn't register any new upgrade steps,
            // these services will be picked up by existing upgrade steps that use
            // analzyers and code fix providers (like the SourceUpdaterStep and
            // RazorUpdaterStep).
            services.Services.AddTransient<DiagnosticAnalyzer, MakeConstAnalyzer>();
            services.Services.AddTransient<CodeFixProvider, MakeConstCodeFixProvider>();
        }
    }
}
