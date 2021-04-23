// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    /// <summary>
    /// Increases backward compatibility by using the Newtonsoft Serializer for ASP.NET Core.
    /// </summary>
    public class NewtonsoftReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private const string NewtonsoftPackageName = "Microsoft.AspNetCore.Mvc.NewtonsoftJson";

        private readonly IPackageLoader _packageLoader;
        private readonly ILogger<NewtonsoftReferenceAnalyzer> _logger;
        private readonly ITargetFrameworkMonikerComparer _tfmComparer;

        public string Name => "Newtonsoft.Json reference analyzer";

        public NewtonsoftReferenceAnalyzer(IPackageLoader packageLoader, ILogger<NewtonsoftReferenceAnalyzer> logger, ITargetFrameworkMonikerComparer tfmComparer)
        {
            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tfmComparer = tfmComparer ?? throw new ArgumentNullException(nameof(tfmComparer));
        }

        public async Task<PackageAnalysisState> AnalyzeAsync(IProject project, PackageAnalysisState state, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);

            // This reference only needs added to ASP.NET Core exes
            if (!(components.HasFlag(ProjectComponents.AspNetCore)
                && project.OutputType == ProjectOutputType.Exe
                && !project.TargetFrameworks.Any(tfm => _tfmComparer.Compare(tfm, TargetFrameworkMoniker.NetCoreApp30) < 0)))
            {
                return state;
            }

            var references = await project.GetNuGetReferencesAsync(token).ConfigureAwait(false);
            if (references.IsTransitivelyAvailable(NewtonsoftPackageName))
            {
                _logger.LogDebug("{PackageName} already referenced transitively", NewtonsoftPackageName);
                return state;
            }

            var packageReferences = references.PackageReferences.Where(r => !state.PackagesToRemove.Contains(r));

            if (!packageReferences.Any(r => NewtonsoftPackageName.Equals(r.Name, StringComparison.OrdinalIgnoreCase)))
            {
                var newtonsoftPackage = await _packageLoader.GetLatestVersionAsync(NewtonsoftPackageName, false, null, token).ConfigureAwait(false);

                if (newtonsoftPackage is not null)
                {
                    _logger.LogInformation("Reference to Newtonsoft package ({NewtonsoftPackageName}, version {NewtonsoftPackageVersion}) needs added", NewtonsoftPackageName, newtonsoftPackage.Version);
                    state.PackagesToAdd.Add(newtonsoftPackage);
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
