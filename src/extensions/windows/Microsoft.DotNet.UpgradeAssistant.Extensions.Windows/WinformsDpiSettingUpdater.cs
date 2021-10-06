// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinForms)]
    public class WinformsDpiSettingUpdater : IUpdater<IProject>
    {
        private const int BufferSize = 65536;
        private const string HighDPIConfiguration = "/configuration/System.Windows.Forms.ApplicationConfigurationSection";

        private readonly WindowsUtilities _utilities = new();
        private readonly ILogger<WinformsDpiSettingUpdater> _logger;

        public string Id => typeof(WinformsDpiSettingUpdater).FullName;

        public string Title => "Winforms Source Updater";

        public string Description => "Update Winforms Program.cs with HighDpiSetting";

        public BuildBreakRisk Risk => BuildBreakRisk.Low;

        public WinformsDpiSettingUpdater(ILogger<WinformsDpiSettingUpdater> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string ProcessAppConfigFile(IProject project)
        {
            var hdpiValue = "SystemAware";
            var appConfigFilePath = project.FindFiles("App.config").FirstOrDefault();
            if (File.Exists(appConfigFilePath))
            {
                var appConfigFile = new ConfigFile(appConfigFilePath);
                var highDPISetting = appConfigFile.Contents.XPathSelectElement(HighDPIConfiguration);
                if (highDPISetting is not null)
                {
                    var hdpi = highDPISetting.Elements("add").Where(e => e.Attribute("key").Value == "DpiAwareness").FirstOrDefault();
                    if (hdpi is not null)
                    {
                        hdpiValue = hdpi.Attribute("value").Value;
                        _logger.LogDebug("Found DpiAwareness Setting with {value}", hdpiValue);
                    }
                }
            }

            return hdpiValue;
        }

        private async Task UpdateHighDPISetting(IProject project)
        {
            var programFile = project.FindFiles("Program.cs").FirstOrDefault();
            if (programFile is not null && File.Exists(programFile))
            {
                var programFileContent = File.ReadAllLines(programFile);

                if (programFileContent.Where(x => x.Contains("Application.SetHighDpiMode")).FirstOrDefault() is null)
                {
                    var hdpiValue = ProcessAppConfigFile(project);
                    try
                    {
                        using var outputStream = File.Create(programFile, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

                        await StreamHelpers.CopyStreamWithNewLineAdded(programFileContent, outputStream, Resources.EnableVisualStylesLine, SR.Format(Resources.HighDPISettingLine, hdpiValue)).ConfigureAwait(false);

                        _logger.LogInformation("Updated Program.cs file at {Path} with HighDPISetting set to {hdpi}", programFile, hdpiValue);
                    }
                    catch (IOException exc)
                    {
                        _logger.LogCritical(exc, "Error while editing Program.cs {Path}", programFile);
                    }
                }
                else
                {
                    _logger.LogInformation("Program.cs file at {Path} already contains HighDPISetting, no need to edit.", programFile);
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
                if (await _utilities.IsWinFormsProjectAsync(project, token))
                {
                    _logger.LogWarning(Resources.HighDPIMessage);
                    fileLocations.Add(Path.Combine(project.FileInfo.DirectoryName, project.FileInfo.Name));
                    await UpdateHighDPISetting(project);
                }
            }

            return new WinformsUpdaterResult(fileLocations.Any(), Resources.DefFontMessage, fileLocations);
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
                    _logger.LogWarning(Resources.HighDPIMessage);
                    fileLocations.Add(Path.Combine(project.FileInfo.DirectoryName, project.FileInfo.Name));
                }
            }

            return new WinformsUpdaterResult(fileLocations.Any(), Resources.DefFontMessage, fileLocations);
        }
    }
}
