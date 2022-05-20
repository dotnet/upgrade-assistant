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
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    internal class WinUIUnnecessaryFilesUpdater : IUpdater<IProject>
    {
        public const string RuleID = "UA303";

        public string Id => typeof(WinUIUnnecessaryFilesUpdater).FullName;

        public string Title => "Remove unnecessary files";

        public string Description => "Removes UWP files no longer required for WinUI";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        private readonly ILogger<WinUIUnnecessaryFilesUpdater> _logger;

        private readonly List<string>? _filesToDelete;

        public WinUIUnnecessaryFilesUpdater(ILogger<WinUIUnnecessaryFilesUpdater> logger, IOptions<WinUIOptions> options)
        {
            this._logger = logger;
            this._filesToDelete = options.Value.FilesToDelete;
        }

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            await Task.Yield();
            if (this._filesToDelete == null || this._filesToDelete.Count == 0)
            {
                return new WindowsDesktopUpdaterResult(
                  RuleID,
                  RuleName: Id,
                  FullDescription: Title,
                  false,
                  "No files to delete",
                  new List<string>());
            }

            foreach (var project in inputs)
            {
                var filesFoundToDelete = new List<string>();
                foreach (var file in this._filesToDelete)
                {
                    filesFoundToDelete.AddRange(project.FindFiles(file));
                }

                foreach (var file in filesFoundToDelete)
                {
                    if (file != null)
                    {
                        _logger.LogInformation($"Deleting {file} as it is not required for Windows App SDK projects.");
                        File.Delete(file);
                    }
                }
            }

            return new WindowsDesktopUpdaterResult(
                RuleID,
                RuleName: Id,
                FullDescription: Title,
                true,
                string.Empty,
                new List<string>());
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            await Task.Yield();
            if (this._filesToDelete == null || this._filesToDelete.Count == 0)
            {
                return new WindowsDesktopUpdaterResult(
                  RuleID,
                  RuleName: Id,
                  FullDescription: Title,
                  false,
                  string.Empty,
                  new List<string>());
            }

            return new WindowsDesktopUpdaterResult(
               RuleID,
               RuleName: Id,
               FullDescription: Title,
               true,
               string.Empty,
               new List<string>());
        }
    }
}
