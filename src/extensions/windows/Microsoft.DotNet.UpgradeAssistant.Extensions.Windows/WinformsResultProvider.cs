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
        private ILogger Logger { get; }

        private readonly IUpdater<IProject> _updater;
        private readonly string _id = "UA102";

        public string ToolName => "Component Analysis";

        public Uri InformationURI => new("https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview");

        public WinformsResultProvider(IUpdater<IProject> updater,
           ILogger<WinformsResultProvider> logger)
        {
            Logger = logger;
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
            var updaterResult = new WinformsUpdaterResult(false, string.Empty, new List<string>());
            try
            {
                var result = await _updater.IsApplicableAsync(context, projects, token).ConfigureAwait(false);
                updaterResult = (WinformsUpdaterResult)result;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exc)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogCritical(exc, "Unexpected exception analyzing winforms references");
            }

            if (updaterResult.Result)
            {
                foreach (var s in updaterResult.FileLocations)
                {
                    yield return new()
                    {
                        RuleId = this._id,
                        RuleName = this.ToolName,
                        FileLocation = s,
                        ResultMessage = updaterResult.Message,
                    };
                }
            }
            else
            {
                Logger.LogInformation("Winforms Updater not applicable to the project(s) selected");
            }
        }
    }
}
