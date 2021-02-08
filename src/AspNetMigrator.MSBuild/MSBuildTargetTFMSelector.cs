using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetMigrator.MSBuild
{
    public class MSBuildTargetTFMSelector : ITargetTFMSelector
    {
        private const string NetStandardTFM = "netstandard2.0";
        private const string DefaultCurrentTFMBase = "net5.0";
        private const string DefaultLTSTFMBase = "net5.0";
        private const string WindowsSuffix = "-windows";
        private readonly ITargetFrameworkMonikerComparer _tfmComparer;
        private readonly ILogger<MSBuildTargetTFMSelector> _logger;
        private readonly string _currentTFMBase;
        private readonly string _ltsTFMBase;
        private readonly UpgradeTarget _upgradeTarget;

        private string AppTFMBase => _upgradeTarget == UpgradeTarget.Current ? _currentTFMBase : _ltsTFMBase;

        public MSBuildTargetTFMSelector(MigrateOptions options, IOptions<TFMSelectorOptions> selectorOptions, ITargetFrameworkMonikerComparer tfmComparer, ILogger<MSBuildTargetTFMSelector> logger)
        {
            _tfmComparer = tfmComparer ?? throw new ArgumentNullException(nameof(tfmComparer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentTFMBase = selectorOptions?.Value.CurrentTFMBase ?? DefaultCurrentTFMBase;
            _ltsTFMBase = selectorOptions?.Value.LTSTFMBase ?? DefaultLTSTFMBase;
            _upgradeTarget = options?.UpgradeTarget ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Chooses the most likely target TFM a project should be retargeted to based on its style, output type, dependencies, and
        /// the user's preference of current or LTS.
        /// </summary>
        public TargetFrameworkMoniker SelectTFM(IProject project)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var tfm = new TargetFrameworkMoniker((project.Style, project.OutputType) switch
            {
                (ProjectStyle.WindowsDesktop, _) => $"{AppTFMBase}{WindowsSuffix}",
                (_, ProjectOutputType.WinExe) => $"{AppTFMBase}{WindowsSuffix}",
                (_, ProjectOutputType.Exe) => AppTFMBase,
                (ProjectStyle.Web, _) => AppTFMBase,
                _ => NetStandardTFM
            });

            _logger.LogDebug("Considering TFM {TFM} for project {Project} based on its style and output type ({ProjectStyle}, {ProjectOutputType})", tfm, project.FilePath, project.Style, project.OutputType);

            // If the project depends on another project with a higher version NetCore or NetStandard TFM,
            // use that TFM instead.
            foreach (var dep in project.ProjectReferences)
            {
                if (_tfmComparer.IsCompatible(tfm, dep.TFM))
                {
                    continue;
                }

                if (dep.TFM.IsNetCore || dep.TFM.IsNetStandard)
                {
                    tfm = dep.TFM;
                    _logger.LogDebug("Considering TFM {TFM} for project {Project} based on its dependency on {DepProject}", tfm, project.FilePath, dep.FilePath);
                }
            }

            _logger.LogDebug("Recommending TFM {TFM} for project {Project}", tfm, project.FilePath);

            return tfm;
        }
    }
}
