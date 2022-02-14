// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    public class WinUIPropertiesUpdater : IUpdater<IProject>
    {
        public string Id => typeof(WinformsDpiSettingUpdater).FullName;

        public string Title => "WinUI Project Properties Updater";

        public string Description => "Update project properties by removing UWP properties and adding WinUI properties";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        private readonly ILogger<WinUIPropertiesUpdater> _logger;

        public WinUIPropertiesUpdater(ILogger<WinUIPropertiesUpdater> logger)
        {
            this._logger = logger;
        }

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            foreach (var project in inputs)
            {
                var projectFile = project.GetFile();
                projectFile.RemoveProperty("WindowsXamlEnableOverview");
                projectFile.RemoveProperty("AppxPackageSigningEnabled");
                projectFile.RemoveProperty("GenerateAssemblyInfo");
                projectFile.SetPropertyValue("Platforms", "x86;x64;arm64");
                projectFile.SetPropertyValue("ApplicationManifest", "app.manifest");
                projectFile.SetPropertyValue("EnablePreviewMsixTooling", "true");
                projectFile.SetPropertyValue("RuntimeIdentifiers", "win10-x86;win10-x64;win10-arm64");
                projectFile.SetPropertyValue("PublishProfile", "win10-$(Platform).pubxml");
                await projectFile.SaveAsync(token).ConfigureAwait(false);
            }

            return new WinformsUpdaterResult(
                "UA302",
                RuleName: Id,
                FullDescription: Title,
                true,
                "",
                new List<string>());
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            return new WinformsUpdaterResult(
                "UA302",
                RuleName: Id,
                FullDescription: Title,
                true,
                "",
                new List<string>());
        }
    }
}
