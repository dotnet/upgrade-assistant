// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Web
{
    /// <summary>
    /// Increases backward compatibility by using the Newtonsoft Serializer for ASP.NET Core.
    /// </summary>
    public class NewtonsoftReferenceAnalyzer : IDependencyAnalyzer
    {
        private const string NewtonsoftPackageName = "Microsoft.AspNetCore.Mvc.NewtonsoftJson";

        private readonly IPackageLoader _packageLoader;
        private readonly ILogger<NewtonsoftReferenceAnalyzer> _logger;
        private readonly ITargetFrameworkMonikerComparer _tfmComparer;
        private readonly ITransitiveDependencyIdentifier _transitiveIdentifier;

        public string Name => "Newtonsoft.Json reference analyzer";

        public NewtonsoftReferenceAnalyzer(
            IPackageLoader packageLoader,
            ITargetFrameworkMonikerComparer tfmComparer,
            ITransitiveDependencyIdentifier transitiveIdentifier,
            ILogger<NewtonsoftReferenceAnalyzer> logger)
        {
            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tfmComparer = tfmComparer ?? throw new ArgumentNullException(nameof(tfmComparer));
            _transitiveIdentifier = transitiveIdentifier ?? throw new ArgumentNullException(nameof(transitiveIdentifier));
        }

        public async Task AnalyzeAsync(IProject project, IDependencyAnalysisState state, CancellationToken token)
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
                && !state.TargetFrameworks.Any(tfm => _tfmComparer.Compare(tfm, TargetFrameworkMoniker.NetCoreApp30) < 0)))
            {
                return;
            }

            if (await _transitiveIdentifier.IsTransitiveDependencyAsync(NewtonsoftPackageName, project, token).ConfigureAwait(false))
            {
                _logger.LogDebug("{PackageName} already referenced transitively", NewtonsoftPackageName);
                return;
            }

            if (!state.Packages.Any(r => NewtonsoftPackageName.Equals(r.Name, StringComparison.OrdinalIgnoreCase)))
            {
                var newtonsoftPackage = await _packageLoader.GetLatestVersionAsync(NewtonsoftPackageName, state.TargetFrameworks, new(), token).ConfigureAwait(false);

                if (newtonsoftPackage is not null)
                {
                    var logMessage = SR.Format("Reference to Newtonsoft package ({0}, version {1}) needs to be added", NewtonsoftPackageName, newtonsoftPackage.Version);
                    _logger.LogInformation(logMessage);
                    state.Packages.Add(newtonsoftPackage, new OperationDetails { Risk = BuildBreakRisk.None, Details = new[] { logMessage } });
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
        }
    }
}
