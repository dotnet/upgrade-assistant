// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    public class WinformsResultProvider : IAnalyzeResultProvider
    {
        private ILogger _logger { get; }
        private readonly IUpdater<IProject> _updater;
        private readonly string _id = "UA102";

        public string Name => "Component Analysis";

        public Uri InformationURI => new("https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview");

        public WinformsResultProvider(IUpdater<IProject> updater,
           ILogger<WinformsResultProvider> logger)
        {
            _logger = logger;
            _updater = updater ?? throw new ArgumentNullException(nameof(updater));
        }

        public async Task<bool> IsApplicableAsync(AnalyzeContext analysis, CancellationToken token)
        {
            if (analysis is null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            foreach (var project in analysis.UpgradeContext.Projects)
            {
                if (await project.IsApplicableAsync(_updater, token).ConfigureAwait(false))
                {
                    return true;
                }
            }

            return false;
        }

        public async IAsyncEnumerable<AnalyzeResult> AnalyzeAsync(AnalyzeContext analysis, [EnumeratorCancellation] CancellationToken token)
        {
            if (analysis is null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            var context = analysis.UpgradeContext;
            var projects = context.Projects.ToImmutableArray();
            var results = new HashSet<AnalyzeResult>();

            try
            {
                var updaterResult = (WinformsUpdaterResult)(await _updater.IsApplicableAsync(context, projects, token).ConfigureAwait(false));
                if (updaterResult.Result)
                {
                    foreach (var s in updaterResult.FileLocations)
                    {
                        results.Add(new()
                        {
                            RuleId = this._id,
                            RuleName = this.Name,
                            FileLocation = s,
                            ResultMessage = updaterResult.Message,
                        });
                    }
                }
                else
                {
                    _logger.LogInformation("Winforms Updater not applicable to the project(s) selected");
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exc)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogCritical(exc, "Unexpected exception analyzing winforms references");
            }

            foreach (var r in results)
            {
                yield return r;
            }
        }
    }
}
