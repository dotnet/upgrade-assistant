// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    internal class WinUINamespaceUpdater : IUpdater<IProject>
    {
        private const string RuleId = "UA301";

        public string Id => typeof(WinUINamespaceUpdater).FullName;

        public string Title => "Update WinUI namespaces";

        public string Description => "Update the namespace for APIs that live in different namespaces in Windows App SDK";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        private readonly Dictionary<string, string>? _namespaceUpdates;

        public WinUINamespaceUpdater(IOptions<WinUIOptions> options)
        {
            this._namespaceUpdates = options.Value.NamespaceUpdates;
        }

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            await Task.Yield();
            if (_namespaceUpdates == null)
            {
                return new WindowsDesktopUpdaterResult(
                    RuleId, RuleName: Id, FullDescription: Title, false, string.Empty, ImmutableList<string>.Empty);
            }

            foreach (var project in inputs)
            {
                foreach (var itemPath in project.FindFiles(".cs", ProjectItemType.Compile))
                {
                    var contents = new StringBuilder(File.ReadAllText(itemPath));
                    foreach (var nameplaceReplace in _namespaceUpdates)
                    {
                        contents.Replace(nameplaceReplace.Key.Trim(), nameplaceReplace.Value.Trim());
                    }

                    File.WriteAllText(itemPath, contents.ToString());
                }

                var oldAppXamlCs = project.FindFiles("App.xaml.old.cs").FirstOrDefault();
                var newAppXamlCs = project.FindFiles("App.xaml.cs").FirstOrDefault();
                if (!string.IsNullOrEmpty(oldAppXamlCs) && !string.IsNullOrEmpty(newAppXamlCs))
                {
                    var oldContent = File.ReadAllText(oldAppXamlCs);
                    var newContent = File.ReadAllText(newAppXamlCs);
                    var rootNamespace = context.CurrentProject?.GetFile().GetPropertyValue("RootNamespace");
                    var match = Regex.Match(oldContent.Replace("\n", string.Empty), "namespace([a-zA-Z_\\d]|\\s)*");
                    if (match.Success && match.Groups.Count > 0 && rootNamespace != null)
                    {
                        var namespaceToUse = match.Groups[0].Value.Trim();
                        newContent = newContent.Replace($"namespace {rootNamespace}", namespaceToUse);
                        File.WriteAllText(newAppXamlCs, newContent);
                    }
                }
            }

            return new WindowsDesktopUpdaterResult(
                RuleId,
                RuleName: Id,
                FullDescription: Title,
                true,
                string.Empty,
                ImmutableList<string>.Empty);
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            await Task.Yield();
            if (_namespaceUpdates == null || !_namespaceUpdates.Any())
            {
                return new WindowsDesktopUpdaterResult(
                    RuleId,
                    RuleName: Id,
                    FullDescription: Title,
                    false,
                    "No namespaces to update",
                    new List<string>());
            }

            var filesToUpdate = new List<string>();
            foreach (var project in inputs)
            {
                foreach (var itemPath in project.FindFiles(".cs", ProjectItemType.Compile))
                {
                    var content = File.ReadAllText(itemPath);
                    foreach (var nameplaceReplace in _namespaceUpdates)
                    {
                        if (content.Contains(nameplaceReplace.Key))
                        {
                            filesToUpdate.Add(itemPath);
                        }
                    }
                }
            }

            return new WindowsDesktopUpdaterResult(
                RuleId,
                RuleName: Id,
                FullDescription: Title,
                true,
                "Namespaces need to be updated",
                filesToUpdate);
        }
    }
}
