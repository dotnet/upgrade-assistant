// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    public class NewtonsoftReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private const string NewtonsoftPackageName = "Microsoft.AspNetCore.Mvc.NewtonsoftJson";

        private readonly IPackageLoader _packageLoader;
        private readonly ILogger<NewtonsoftReferenceAnalyzer> _logger;

        public string Name => "Newtonsoft.Json reference analyzer";

        public NewtonsoftReferenceAnalyzer(IPackageLoader packageLoader, ILogger<NewtonsoftReferenceAnalyzer> logger)
        {
            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PackageAnalysisState> AnalyzeAsync(IProject project, PackageAnalysisState state, CancellationToken token)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var packageReferences = project.Required().PackageReferences.Where(r => !state.PackagesToRemove.Contains(r));

            if (project != null && (project.Components & ProjectComponents.Web) == ProjectComponents.Web

                // If the web project doesn't include a reference to the Newtonsoft package, mark it for addition
                && !packageReferences.Any(r => NewtonsoftPackageName.Equals(r.Name, StringComparison.OrdinalIgnoreCase)))
            {
                var analyzerPackage = await _packageLoader.GetLatestVersionAsync(NewtonsoftPackageName, false, null, token).ConfigureAwait(false);

                if (analyzerPackage is not null)
                {
                    _logger.LogInformation("Reference to Newtonsoft package ({NewtonsoftPackageName}, version {NewtonsoftPackageVersion}) needs added", NewtonsoftPackageName, analyzerPackage.Version);
                    state.PackagesToAdd.Add(analyzerPackage);
                }
                else
                {
                    _logger.LogWarning("Newtonsoft NuGet package reference cannot be added because the package cannot be found");
                }
            }
            else
            {
                _logger.LogDebug("Reference to Newtonsoft package ({NewtonsoftPackageName}) already exists", NewtonsoftPackageName);
            }

            return state;
        }
    }
}
