// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinForms)]
    public class WinformsDefaultFontUpdater : IUpdater<IProject>
    {
        private readonly WindowsUtilities _utilities = new();
        private readonly ILogger<WinformsDefaultFontUpdater> _logger;

        public string Id => typeof(WinformsDefaultFontUpdater).FullName;

        public string Title => "Default Font API Alert";

        public string Description => "Default font in Windows Forms has been changed from Microsoft Sans Serif to Seg Segoe UI, in order to change the default font use the API - Application.SetDefaultFont(Font font). For more details see here - https://devblogs.microsoft.com/dotnet/whats-new-in-windows-forms-in-net-6-0-preview-5/#application-wide-default-font";

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
                if (await _utilities.IsWinFormsProjectAsync(project, token))
                {
                    _logger.LogWarning(this.Description);
                    fileLocations.Add(Path.Combine(project.FileInfo.DirectoryName, project.FileInfo.Name));
                }
            }

            return new WinformsUpdaterResult(fileLocations.Any(), this.Description, fileLocations);
        }
    }
}
