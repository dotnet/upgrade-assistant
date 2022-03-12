// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    internal class WinUINamespaceUpdater : IUpdater<IProject>
    {
        public string Id => typeof(WinformsDpiSettingUpdater).FullName;

        public string Title => "UWP Namespace updater";

        public string Description => "Update namespace to use WindowsAppSDK APIs";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        private readonly ILogger<WinUINamespaceUpdater> _logger;

        public WinUINamespaceUpdater(ILogger<WinUINamespaceUpdater> logger)
        {
            this._logger = logger;
        }

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            foreach (var project in inputs)
            {
                foreach (var itemPath in project.FindFiles(".cs", ProjectItemType.Compile))
                {
                    var contents = File.ReadAllText(itemPath);
                    contents = contents
                        .Replace("Windows.UI.", "Microsoft.UI.")
                        .Replace("Microsoft.UI.Core", "Windows.UI.Core")
                        .Replace("Microsoft.UI.ViewManagement", "Windows.UI.ViewManagement")
                        .Replace("Microsoft.Toolkit.Uwp.UI.Animations", "CommunityToolkit.WinUI.UI.Animations")
                        .Replace("Window.Current.Compositor", "App.Window.Compositor");
                    File.WriteAllText(itemPath, contents);
                }

                foreach (var itemPath in project.FindFiles(".xaml", ProjectItemType.None))
                {
                    var contents = File.ReadAllText(itemPath);
                    contents = contents
                        .Replace("Microsoft.Toolkit.Uwp.UI.Animations", "CommunityToolkit.WinUI.UI.Animations")
                        .Replace("animations:ReorderGridAnimation", "animations:ItemsReorderAnimation");
                    var doc = new XmlDocument();
                    doc.Load(itemPath);
                    var x = doc.DocumentElement;
                    File.WriteAllText(itemPath, contents);
                }
            }

            return new WinformsUpdaterResult(
                "UA301",
                RuleName: Id,
                FullDescription: Title,
                true,
                "",
                new List<string>());
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            return new WinformsUpdaterResult(
                "UA301",
                RuleName: Id,
                FullDescription: Title,
                true,
                "",
                new List<string>());
        }
    }
}
