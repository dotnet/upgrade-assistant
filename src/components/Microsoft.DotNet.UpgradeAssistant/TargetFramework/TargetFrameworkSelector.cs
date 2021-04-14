// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.TargetFramework
{
    public class TargetFrameworkSelector : ITargetFrameworkSelector
    {
        private const string DefaultCurrentTFMBase = "net5.0";
        private const string DefaultLTSTFMBase = "net5.0";

        private readonly ITargetFrameworkMonikerComparer _comparer;
        private readonly IEnumerable<ITargetFrameworkSelectorFilter> _selectors;

        private readonly string _currentTFMBase;
        private readonly string _ltsTFMBase;

        private readonly UpgradeTarget _upgradeTarget;

        public TargetFrameworkSelector(UpgradeOptions options, ITargetFrameworkMonikerComparer comparer, IOptions<TFMSelectorOptions> selectorOptions, IEnumerable<ITargetFrameworkSelectorFilter> selectors)
        {
            _comparer = comparer;
            _selectors = selectors;

            _currentTFMBase = selectorOptions?.Value.CurrentTFMBase ?? DefaultCurrentTFMBase;
            _ltsTFMBase = selectorOptions?.Value.LTSTFMBase ?? DefaultLTSTFMBase;
            _upgradeTarget = options?.UpgradeTarget ?? throw new ArgumentNullException(nameof(options));
        }

        public async ValueTask<TargetFrameworkMoniker> SelectTargetFrameworkAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var appBase = _upgradeTarget == UpgradeTarget.Current ? _currentTFMBase : _ltsTFMBase;
            var current = GetDefaultTargetFrameworkMoniker(project);
            var appBaseTfm = new TargetFrameworkMoniker(appBase);

            var updater = new FilterState(_comparer, project, current, appBaseTfm)
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
            public FilterState(ITargetFrameworkMonikerComparer comparer, IProject project, TargetFrameworkMoniker current, TargetFrameworkMoniker appbase)
            {
                Comparer = comparer;
                Project = project;
                Current = current;
                AppBase = appbase;
            }

            public ITargetFrameworkMonikerComparer Comparer { get; }

            public TargetFrameworkMoniker Current { get; private set; }

            public TargetFrameworkMoniker AppBase { get; }

            public IProject Project { get; }

            public ProjectComponents Components { get; init; }

            public bool TryUpdate(TargetFrameworkMoniker tfm)
            {
                if (Comparer.Compare(tfm, Current) > 0)
                {
                    Current = tfm;
                    return true;
                }

                return false;
            }
        }
    }
}
