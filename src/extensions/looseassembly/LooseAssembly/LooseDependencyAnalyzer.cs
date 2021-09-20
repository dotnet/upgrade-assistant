// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly
{
    public class LooseDependencyAnalyzer : IDependencyAnalyzer
    {
        private readonly NuGetPackageLookup _lookup;
        private readonly ILogger<LooseDependencyAnalyzer> _logger;

        public LooseDependencyAnalyzer(
            NuGetPackageLookup lookup,
            ILogger<LooseDependencyAnalyzer> logger)
        {
            _lookup = lookup;
            _logger = logger;
        }

        public string Name => "Loose dependency analyzer";

        public async Task AnalyzeAsync(IProject project, IDependencyAnalysisState state, CancellationToken token)
        {
            foreach (var reference in state.References)
            {
                if (project.TryResolveHintPath(reference, out var path))
                {
                    _logger.LogDebug("Found hint path for {Reference} at {Path}", reference.Name, path);

                    var found = await _lookup.SearchAsync(path, project.TargetFrameworks, token).ToListAsync(token);

                    if (found.Count == 0)
                    {
                        _logger.LogWarning("No match found for loose assembly {Path}", reference);
                        continue;
                    }

                    if (found.Count > 1)
                    {
                        _logger.LogWarning("Multiple matches found for {Path}", reference);
                    }

                    var first = found.FirstOrDefault();

                    if (first is not null)
                    {
                        _logger.LogDebug("Found package {Package}: {Version}", first.Name, first.Version);

                        state.References.Remove(reference, new OperationDetails { Risk = BuildBreakRisk.Medium, Details = new[] { "Found package" } });
                        state.Packages.Add(first, new OperationDetails { Risk = BuildBreakRisk.Medium });
                    }
                }
            }
        }
    }
}
