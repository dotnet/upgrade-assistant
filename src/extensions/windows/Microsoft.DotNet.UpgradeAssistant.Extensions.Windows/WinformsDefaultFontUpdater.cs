// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinForms)]
    public class WinformsDefaultFontUpdater : IUpdater<IProject>
    {
        private const string RuleId = "UA209";

        private readonly ILogger<WinformsDefaultFontUpdater> _logger;

        public string Id => typeof(WinformsDefaultFontUpdater).FullName;

        public string Title => "Default Font API Alert";

        public string Description => Resources.DefaultFontMessage;

        public BuildBreakRisk Risk => BuildBreakRisk.Low;

        public WinformsDefaultFontUpdater(ILogger<WinformsDefaultFontUpdater> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            return this.IsApplicableAsync(context, inputs, token);
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var fileLocations = new List<string>();

            foreach (var project in inputs)
            {
                if (await project.IsWinFormsProjectAsync(token).ConfigureAwait(false))
                {
                    _logger.LogWarning(this.Description);
                    fileLocations.Add(Path.Combine(project.FileInfo.DirectoryName, project.FileInfo.Name));
                }
            }

            return new WindowsDesktopUpdaterResult(
                RuleId,
                RuleName: Id,
                FullDescription: Title,
                Result: fileLocations.Any(),
                Message: this.Description,
                FileLocations: fileLocations);
        }
    }
}
