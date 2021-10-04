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
    public class WinformsDefaultFontUpdater : IUpdater<IProject>
    {
        private const int BufferSize = 65536;
        private const string HighDPIConfiguration = "/configuration/System.Windows.Forms.ApplicationConfigurationSection";

        private readonly WindowsUtilities _utilities = new();
        private readonly ILogger<WinformsDefaultFontUpdater> _logger;
        private readonly string _winformsSourceUpdateInfo = "Default font in Windows Forms has been changed from Microsoft Sans Serif to Seg Segoe UI, in order to change the default font use the API - Application.SetDefaultFont(Font font). For more details see here - https://devblogs.microsoft.com/dotnet/whats-new-in-windows-forms-in-net-6-0-preview-5/#application-wide-default-font. \nIts recommended to set HighDpiMode to be SystemAware for better results in Main() - Application.SetHighDpiMode(HighDpiMode.SystemAware)";

        public string Id => typeof(WinformsDefaultFontUpdater).FullName;

        public string Title => "Winforms Source Updater";

        public string Description => "Update code for Winforms project documents";

        public BuildBreakRisk Risk => BuildBreakRisk.Low;

        public WinformsDefaultFontUpdater(ILogger<WinformsDefaultFontUpdater> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string ProcessAppConfigFile(IProject project)
        {
            var hdpiValue = "SystemAware";
            var appConfigFilePath = project.FindFiles("App.config").FirstOrDefault();
            ConfigFile appConfigFile;
            if (File.Exists(appConfigFilePath))
            {
                appConfigFile = new ConfigFile(appConfigFilePath);
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
            var programFileContent = File.ReadAllLines(programFile);

            if (!programFileContent.Contains("Application.SetHighDpiMode"))
            {
                var hdpiValue = ProcessAppConfigFile(project);
                try
                {
                    using var outputStream = File.Create(programFile, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

                    await StreamHelpers.CopyStreamWithNewLineAdded(programFileContent, outputStream, "Application.EnableVisualStyles()", SR.Format("Application.SetHighDpiMode(HighDpiMode.{0});", hdpiValue)).ConfigureAwait(false);
                }
                catch (IOException exc)
                {
                    _logger.LogCritical(exc, "Error while editing Program.cs {Path}", programFile);
                }

                _logger.LogInformation("Updated Program.cs file at {Path} with HighDPISetting set to {hdpi}", programFile, hdpiValue);
            }
            else
            {
                _logger.LogInformation("Program.cs file at {Path} already contains HighDPISetting, no need to edit.", programFile);
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
                    _logger.LogWarning(_winformsSourceUpdateInfo);
                    fileLocations.Add(Path.Combine(project.FileInfo.DirectoryName, project.FileInfo.Name));
                    await UpdateHighDPISetting(project);
                }
            }

            return new WinformsUpdaterResult(fileLocations.Any(), _winformsSourceUpdateInfo, fileLocations);
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
                    _logger.LogWarning(_winformsSourceUpdateInfo);
                    fileLocations.Add(Path.Combine(project.FileInfo.DirectoryName, project.FileInfo.Name));
                }
            }

            return new WinformsUpdaterResult(fileLocations.Any(), _winformsSourceUpdateInfo, fileLocations);
        }
    }
}
