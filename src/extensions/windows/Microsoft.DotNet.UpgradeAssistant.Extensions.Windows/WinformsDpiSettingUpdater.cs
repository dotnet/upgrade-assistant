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
    public class WinformsDpiSettingUpdater : IUpdater<IProject>
    {
        private const string RuleId = "UA202";
        private const int BufferSize = 65536;

        private readonly ILogger<WinformsDpiSettingUpdater> _logger;
        private ProgramFileSpec _programFileSpec = new();

        public string Id => typeof(WinformsDpiSettingUpdater).FullName;

        public string Title => "Winforms Source Updater";

        public string Description => "Update Winforms Program.cs with HighDpiSetting";

        public BuildBreakRisk Risk => BuildBreakRisk.Low;

        public WinformsDpiSettingUpdater(ILogger<WinformsDpiSettingUpdater> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static string ProcessAppConfigFile(IProject project)
        {
            var appConfigFilePath = project.FindFiles("App.config").FirstOrDefault();
            var hdpiValue = WindowsUtilities.GetElementFromAppConfig(appConfigFilePath, Resources.HighDPIConfiguration, Resources.HighDpiSettingKey);
            return string.IsNullOrEmpty(hdpiValue) ? Resources.HighDpiDefaultSetting : hdpiValue;
        }

        public async Task UpdateHighDPISetting(IProject project, string[] programFileContent, bool isDpiSettingSetInProgramFile, string programFilePath)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (!programFileContent.Any())
            {
                _logger.LogInformation("No Program.cs file found at {Path}.", programFilePath);
            }
            else
            {
                if (!isDpiSettingSetInProgramFile)
                {
                    var hdpiValue = ProcessAppConfigFile(project);
                    try
                    {
                        using var outputStream = File.Create(programFilePath, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

                        await StreamHelpers.CopyStreamWithNewLineAdded(programFileContent, outputStream, Resources.EnableVisualStylesLine, SR.Format(Resources.HighDPISettingLine, hdpiValue)).ConfigureAwait(false);

                        _logger.LogInformation("Updated Program.cs file at {Path} with HighDPISetting set to {HighDpi}", programFilePath, hdpiValue);
                    }
                    catch (IOException exc)
                    {
                        _logger.LogCritical(exc, "Error while editing Program.cs {Path}", programFilePath);
                    }
                }
                else
                {
                    _logger.LogInformation("Program.cs file at {Path} already contains HighDPISetting, no need to edit.", programFilePath);
                }
            }
        }

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
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
                    _programFileSpec = new(project.FindFiles("Program.cs").FirstOrDefault());
                    if (!_programFileSpec.IsDpiSettingSet && _programFileSpec.FileContent.Any())
                    {
                        _logger.LogWarning(Resources.HighDPIMessage);
                        fileLocations.Add(_programFileSpec.Path);
                    }

                    await UpdateHighDPISetting(project, _programFileSpec.FileContent, _programFileSpec.IsDpiSettingSet, _programFileSpec.Path).ConfigureAwait(false);
                }
            }

            return new WindowsDesktopUpdaterResult(
                RuleId,
                RuleName: Id,
                FullDescription: Title,
                fileLocations.Any(),
                Resources.HighDPIMessage,
                fileLocations);
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
                    _programFileSpec = new(project.FindFiles("Program.cs").FirstOrDefault());
                    if (!_programFileSpec.IsDpiSettingSet && _programFileSpec.FileContent.Any())
                    {
                        _logger.LogWarning(Resources.HighDPIMessage);
                        fileLocations.Add(_programFileSpec.Path);
                    }
                }
            }

            return new WindowsDesktopUpdaterResult(
                RuleId,
                RuleName: Id,
                FullDescription: Title,
                fileLocations.Any(),
                Resources.HighDPIMessage,
                fileLocations);
        }
    }
}
