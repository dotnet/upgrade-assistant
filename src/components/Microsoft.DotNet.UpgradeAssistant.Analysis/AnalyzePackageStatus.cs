using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages;
using Microsoft.DotNet.UpgradeAssistant.Steps.Solution;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public class AnalyzePackageStatus : IAnalyzeResultProvider
    {
        private readonly IDependencyAnalyzerRunner _packageAnalyzer;
        private readonly IEntrypointResolver _entrypointResolver;
        private readonly UpgradeOptions _options;

        private IDependencyAnalysisState? _analysisState;

        protected ILogger Logger { get; }

        public AnalyzePackageStatus(UpgradeOptions options,
            IDependencyAnalyzerRunner packageAnalyzer,
            ILogger<AnalyzePackageStatus> logger,
            IEntrypointResolver entrypointResolver)
        {
            Logger = logger;
            _options = options;
            _packageAnalyzer = packageAnalyzer ?? throw new ArgumentNullException(nameof(packageAnalyzer));
            _entrypointResolver = entrypointResolver ?? throw new ArgumentNullException(nameof(entrypointResolver));
            _analysisState = null;
        }

        public async Task AnalyzeAsync(AnalyzeContext analysis, CancellationToken token)
        {
            if (analysis is null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            var context = analysis.UpgradeContext;
            var projects = context.Projects.ToList();

            if (!context.InputIsSolution)
            {
                var selectedProject = projects.First(i => i.FileInfo.Name.Equals(Path.GetFileName(context.InputPath), StringComparison.OrdinalIgnoreCase));
                context.EntryPoints = new[] { selectedProject };
            }
            else
            {
                context.EntryPoints = _entrypointResolver.GetEntrypoints(context.Projects, _options.EntryPoint.Any() ? _options.EntryPoint : new[] { "*" });
            }

            foreach (var project in context.EntryPoints)
            {
                try
                {
                    _analysisState = await _packageAnalyzer.AnalyzeAsync(context, project, token).ConfigureAwait(false);
                    if (!_analysisState.IsValid)
                    {
                        Logger.LogError($"Package analysis failed");
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception exc)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    Logger.LogCritical(exc, "Unexpected exception analyzing package references for: {ProjectPath}", context.CurrentProject.Required().FileInfo);
                }

                if (_analysisState is null || !_analysisState.AreChangesRecommended)
                {
                    Logger.LogInformation("No package updates needed");
                }
                else
                {
                    Logger.LogInformation("Package Analysis for the {ProjectPath}", project.FileInfo);
                    LogDetails("References to be removed: {References}", _analysisState.References.Deletions);
                    LogDetails("References to be added: {References}", _analysisState.References.Additions);
                    LogDetails("Packages to be removed: {Packages}", _analysisState.Packages.Deletions);
                    LogDetails("Packages to be added: {Packages}", _analysisState.Packages.Additions);
                    LogDetails("Framework references to be added: {FrameworkReference}", _analysisState.FrameworkReferences.Additions);
                    LogDetails("Framework references to be removed: {FrameworkReference}", _analysisState.FrameworkReferences.Deletions);

                    void LogDetails<T>(string name, IReadOnlyCollection<T> collection)
                    {
                        if (collection.Count > 0)
                        {
                            Logger.LogInformation(name, string.Join(Environment.NewLine, collection));
                        }
                    }
                }
            }
        }
    }
}
