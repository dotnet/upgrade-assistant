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
        private readonly ILogger<MSBuildTargetTFMSelector> _logger;
        private readonly TFMSelectorOptions _selectorOptions;
        private readonly UpgradeTarget _upgradeTarget;

        public MSBuildTargetTFMSelector(MigrateOptions options, IOptions<TFMSelectorOptions> selectorOptions, ILogger<MSBuildTargetTFMSelector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _selectorOptions = selectorOptions?.Value ?? throw new ArgumentNullException(nameof(selectorOptions));
            _upgradeTarget = options?.UpgradeTarget ?? throw new ArgumentNullException(nameof(options));
        }

        public ValueTask<TargetFrameworkMoniker> SelectTFMAsync(IProject project)
        {
            throw new NotImplementedException();
        }
    }
}
