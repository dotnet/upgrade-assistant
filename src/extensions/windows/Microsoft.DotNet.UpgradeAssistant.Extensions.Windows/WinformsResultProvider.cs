// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    public class WinformsResultProvider : IAnalyzeResultProvider
    {
        private readonly IEnumerable<IUpdater<IProject>> _updaters;

        private ILogger Logger { get; }

        public string Name => "Component Analysis";

        public Uri InformationUri => new("https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview");

        public WinformsResultProvider(IEnumerable<IUpdater<IProject>> updaters,
           ILogger<WinformsResultProvider> logger)
        {
            Logger = logger;
            _updaters = updaters ?? throw new ArgumentNullException(nameof(updaters));
        }

        public async Task<bool> IsApplicableAsync(AnalyzeContext analysis, CancellationToken token)
        {
            if (analysis is null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            foreach (var project in analysis.UpgradeContext.Projects)
            {
                if (await project.IsApplicableAsync(_updaters, token).ConfigureAwait(false))
                {
                    return true;
                }
            }

            return false;
        }

        public async IAsyncEnumerable<OutputResult> AnalyzeAsync(AnalyzeContext analysis, [EnumeratorCancellation] CancellationToken token)
        {
            if (analysis is null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            var context = analysis.UpgradeContext;
            var projects = context.Projects.ToImmutableArray();
            var updaterResults = new List<WindowsDesktopUpdaterResult>();
            try
            {
                foreach (var updater in _updaters)
                {
                    var result = await updater.IsApplicableAsync(context, projects, token).ConfigureAwait(false);
                    updaterResults.Add((WindowsDesktopUpdaterResult)result);
                }
            }
            catch (Exception exc)
            {
                Logger.LogCritical(exc, "Unexpected exception analyzing winforms references");
            }

            var applicableUpgraderResults = updaterResults.Where(r => r.Result).ToList();
            if (applicableUpgraderResults.Any())
            {
                foreach (var updaterResult in applicableUpgraderResults)
                {
                    foreach (var s in updaterResult.FileLocations)
                    {
                        yield return new()
                        {
                            RuleId = updaterResult.RuleId,
                            RuleName = updaterResult.RuleName,
                            FullDescription = updaterResult.FullDescription,
                            FileLocation = s,
                            ResultMessage = updaterResult.Message,
                        };
                    }
                }
            }
            else
            {
                Logger.LogInformation("Winforms Updater not applicable to the project(s) selected");
            }
        }
    }
}
