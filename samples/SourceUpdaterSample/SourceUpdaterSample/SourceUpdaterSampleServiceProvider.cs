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
        /// <param name="serviceConfiguration">A configuration object containing both the
        /// service collection to register services in and the extension's configuration file.</param>
        /// <returns>The service collection updated with services Upgrade Assistant should use.</returns>
        public IServiceCollection AddServices(ExtensionServiceConfiguration serviceConfiguration)
        {
            if (serviceConfiguration is null)
            {
                throw new ArgumentNullException(nameof(serviceConfiguration));
            }

            serviceConfiguration.ServiceCollection.AddTransient<DiagnosticAnalyzer, MakeConstAnalyzer>();
            serviceConfiguration.ServiceCollection.AddTransient<CodeFixProvider, MakeConstCodeFixProvider>();

            return serviceConfiguration.ServiceCollection;
        }
    }
}
