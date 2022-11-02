// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.TargetFramework
{
    public class TargetFrameworkSelector : ITargetFrameworkSelector
    {
        private readonly ITargetFrameworkMonikerComparer _comparer;
        private readonly DefaultTfmOptions _selectorOptions;
        private readonly IEnumerable<ITargetFrameworkSelectorFilter> _selectors;
        private readonly ILogger<TargetFrameworkSelector> _logger;

        public TargetFrameworkSelector(
            ITargetFrameworkMonikerComparer comparer,
            IOptions<DefaultTfmOptions> selectorOptions,
            IEnumerable<ITargetFrameworkSelectorFilter> selectors,
            ILogger<TargetFrameworkSelector> logger)
        {
            _comparer = comparer;
            _selectorOptions = selectorOptions?.Value ?? throw new ArgumentNullException(nameof(selectorOptions));
            _selectors = selectors;
            _logger = logger;
        }

        public async ValueTask<TargetFrameworkMoniker> SelectTargetFrameworkAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var appBase = _selectorOptions.TargetTfmSupport switch
            {
                UpgradeTarget.STS => _selectorOptions.STS,
                UpgradeTarget.Preview => _selectorOptions.Preview,
                UpgradeTarget.LTS => _selectorOptions.LTS,
                _ => _selectorOptions.STS,
            };

            var current = GetDefaultTargetFrameworkMoniker(project);
            if (!_comparer.TryParse(appBase, out var appBaseTfm))
            {
                throw new InvalidOperationException("Invalid app base TFM");
            }

            var updater = new FilterState(_comparer, project, current, appBaseTfm, _logger)
            {
                Components = await project.GetComponentsAsync(token).ConfigureAwait(false),
            };

            foreach (var selector in _selectors)
            {
                selector.Process(updater);
            }

            return updater.Current;
        }

        private static TargetFrameworkMoniker GetDefaultTargetFrameworkMoniker(IProject project)
        {
            foreach (var currentTfm in project.TargetFrameworks)
            {
                if (currentTfm.IsNetStandard)
                {
                    return currentTfm;
                }
            }

            return TargetFrameworkMoniker.NetStandard20;
        }

        private class FilterState : ITargetFrameworkSelectorFilterState
        {
            private readonly ITargetFrameworkMonikerComparer _comparer;
            private readonly ILogger _logger;

            public FilterState(ITargetFrameworkMonikerComparer comparer, IProject project, TargetFrameworkMoniker current, TargetFrameworkMoniker appbase, ILogger logger)
            {
                _comparer = comparer;
                Project = project;
                Current = current;
                AppBase = appbase;
                _logger = logger;
            }

            public TargetFrameworkMoniker Current { get; private set; }

            public TargetFrameworkMoniker AppBase { get; }

            public IProject Project { get; }

            public ProjectComponents Components { get; init; }

            public bool TryUpdate(TargetFrameworkMoniker tfm)
            {
                if (_comparer.TryMerge(Current, tfm, out var result))
                {
                    var wasChanged = Current != result;
                    Current = result;
                    return wasChanged;
                }

                _logger.LogWarning("Could not merge incoming TFM update from {Current} to {Next}", Current, tfm);

                return false;
            }
        }
    }
}
