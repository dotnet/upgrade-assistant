// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    internal class WinUIAnimationsXamlUpdater : IUpdater<IProject>
    {
        public const string RuleID = "UA305";

        public string Id => typeof(WinUIAnimationsXamlUpdater).FullName;

        public string Title => "Update animations xaml";

        public string Description => "Update the animations xaml to fix ReorderGridAnimation.Duration attrubute";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            await Task.Yield();
            foreach (var project in inputs)
            {
                foreach (var file in project.FindFiles(".xaml", ProjectItemType.None))
                {
                    var content = File.ReadAllText(file);
                    content = content.Replace("using:Microsoft.Toolkit.Uwp.UI.Animations", "using:CommunityToolkit.WinUI.UI.Animations");

                    var doc = new XmlDocument();
                    doc.LoadXml(content);

                    Dictionary<string, string> xmlNamespaces = new Dictionary<string, string>()
                    {
                        { "def",  "http://schemas.microsoft.com/winfx/2006/xaml/presentation" },
                        { "animations", "using:CommunityToolkit.WinUI.UI.Animations" },
                    };
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                    foreach (var ns in xmlNamespaces)
                    {
                        nsmgr.AddNamespace(ns.Key, ns.Value);
                    }

                    var elements = doc.SelectNodes("//*[@animations:ReorderGridAnimation.Duration]", nsmgr);
                    foreach (XmlElement element in elements)
                    {
                        var value = element.GetAttribute("ReorderGridAnimation.Duration", xmlNamespaces["animations"]);
                        if (double.TryParse(value, out var valueNumber))
                        {
                            double newValueNumber = valueNumber / 1000;
                            element.RemoveAttribute("ReorderGridAnimation.Duration", xmlNamespaces["animations"]);
                            element.SetAttribute("ItemsReorderAnimation.Duration", xmlNamespaces["animations"], string.Format(CultureInfo.CurrentCulture, "0:0:{0:0.00}", newValueNumber));
                        }
                    }

                    doc.Save(file);
                }
            }

            return new WindowsDesktopUpdaterResult(
              RuleID,
              RuleName: Id,
              FullDescription: Title,
              true,
              "Update Successful",
              ImmutableList.Create<string>());
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            await Task.Yield();
            var filesToUpdate = new List<string>();
            foreach (var project in inputs)
            {
                foreach (var file in project.FindFiles(".xaml", ProjectItemType.None))
                {
                    var contents = File.ReadAllText(file);
                    if (contents.Contains("ReorderGridAnimation.Duration"))
                    {
                        filesToUpdate.Add(file);
                        continue;
                    }
                }
            }

            return new WindowsDesktopUpdaterResult(
               RuleID,
               RuleName: Id,
               FullDescription: Title,
               filesToUpdate.Any(),
               string.Empty,
               filesToUpdate);
        }
    }
}
