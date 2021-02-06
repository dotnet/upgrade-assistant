using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private readonly ILogger<MSBuildTargetTFMSelector> _logger;
        private readonly string _currentTFMBase;
        private readonly string _ltsTFMBase;
        private readonly UpgradeTarget _upgradeTarget;

        public MSBuildTargetTFMSelector(MigrateOptions options, IOptions<TFMSelectorOptions> selectorOptions, ILogger<MSBuildTargetTFMSelector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentTFMBase = selectorOptions?.Value.CurrentTFMBase ?? DefaultCurrentTFMBase;
            _ltsTFMBase = selectorOptions?.Value.LTSTFMBase ?? DefaultLTSTFMBase;
            _upgradeTarget = options?.UpgradeTarget ?? throw new ArgumentNullException(nameof(options));
        }

        public TargetFrameworkMoniker HighestPossibleTFM => new TargetFrameworkMoniker($"{_currentTFMBase}{WindowsSuffix}");

        public async ValueTask<TargetFrameworkMoniker> SelectTFMAsync(IProject project)
        {
            // TODO - Implement this!
            return new TargetFrameworkMoniker(_currentTFMBase);
        }
    }
}
