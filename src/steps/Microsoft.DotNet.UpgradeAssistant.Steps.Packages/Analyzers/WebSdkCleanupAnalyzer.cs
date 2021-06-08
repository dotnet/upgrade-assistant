﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    public class WebSdkCleanupAnalyzer : IDependencyAnalyzer
    {
        private const string AspNetCoreFrameworkReference = "Microsoft.AspNetCore.App";
        private const string WebSdk = "Microsoft.NET.Sdk.Web";

        private readonly ILogger<WebSdkCleanupAnalyzer> _logger;

        public string Name => "Web SDK cleanup analyzer";

        public WebSdkCleanupAnalyzer(ILogger<WebSdkCleanupAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task AnalyzeAsync(IProject project, IReadOnlyCollection<TargetFrameworkMoniker> targetframeworks, IDependencyAnalysisState state, CancellationToken token)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            project = project.Required();
            var projectRoot = project.GetFile();

            // Check SDK directly (instead of using project.Components) since having the FrameworkReference counts as
            // having the AspNetCore component and this analyzer is specifically insterested in cases where both the SDK
            // and the framework reference are present.
            if (!projectRoot.IsSdk || !projectRoot.Sdk.Equals(WebSdk, StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            var aspNetCoreReference = project.FrameworkReferences?.FirstOrDefault(f => f.Name.Equals(AspNetCoreFrameworkReference, StringComparison.OrdinalIgnoreCase));

            if (aspNetCoreReference is not null)
            {
                _logger.LogInformation("Removing framework reference Microsoft.AspNetCore.App it is already included as part of the Microsoft.NET.Sdk.Web SDK");
                state.FrameworkReferences.Remove(aspNetCoreReference);
            }

            return Task.FromResult(state);
        }
    }
}
