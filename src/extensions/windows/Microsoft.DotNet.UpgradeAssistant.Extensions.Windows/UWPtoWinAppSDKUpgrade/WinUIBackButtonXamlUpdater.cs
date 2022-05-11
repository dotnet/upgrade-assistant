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
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    internal class WinUIBackButtonXamlUpdater : IUpdater<IProject>
    {
        public const string NewBackButtonName = "UAGeneratedBackButton";

        public string Id => "UA308";

        public string Title => "Insert back button in XAML";

        public string Description => "Adds a back button to all WinUI XAML pages.";

        private const string AnalysisString = "Default back button in the title bar does not exist in WinUI3 apps. You have to add your own back button.";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            await Task.Yield();
            foreach (var project in inputs)
            {
                foreach (var file in project.FindFiles(".xaml", ProjectItemType.None))
                {
                    var doc = new XmlDocument();
                    doc.Load(file);
                    Dictionary<string, string> xmlNamespaces = new Dictionary<string, string>()
                    {
                        { "def",  "http://schemas.microsoft.com/winfx/2006/xaml/presentation" },
                        { "x", "http://schemas.microsoft.com/winfx/2006/xaml" },
                        { "muxc", "using:Microsoft.UI.Xaml.Controls" },
                        { "d", "http://schemas.microsoft.com/expression/blend/2008" },
                        { "mc", "http://schemas.openxmlformats.org/markup-compatibility/2006" }
                    };
                    var nsmgr = new XmlNamespaceManager(doc.NameTable);
                    foreach (var ns in xmlNamespaces)
                    {
                        nsmgr.AddNamespace(ns.Key, ns.Value);
                    }

                    var root = doc.DocumentElement;
                    if (root == null || !root.Name.Contains("Page"))
                    {
                        continue;
                    }

                    var lastPageNode = root.ChildNodes.Cast<XmlNode>().LastOrDefault();

                    var stackPanel = doc.CreateElement("StackPanel", root.NamespaceURI);
                    stackPanel.SetAttribute("Name", xmlNamespaces["x"], "UAGeneratedPanel");
                    stackPanel.SetAttribute("Orientation", root.NamespaceURI, "Vertical");

                    var backButtonElement = doc.CreateElement("AppBarButton", root.NamespaceURI);
                    backButtonElement.SetAttribute("Name", xmlNamespaces["x"], NewBackButtonName);
                    backButtonElement.SetAttribute("Foreground", root.NamespaceURI, "Black");
                    backButtonElement.SetAttribute("Margin", root.NamespaceURI, "0,0,12,0");

                    var backButtonSymbol = doc.CreateElement("SymbolIcon", root.NamespaceURI);
                    backButtonSymbol.SetAttribute("Symbol", root.NamespaceURI, "Back");
                    backButtonElement.AppendChild(backButtonSymbol);

                    var comment = doc.CreateComment(@$"TODO {WinUIBackButtonAnalyzer.DiagnosticId} Default back button in the title bar does not exist in WinUI3 apps.
                        We have created a custom back button for you. Feel free to rename and edit its position/behavior.
                        Read: https://aka.ms/UWP.NetUpgrade/UA3015");
                    stackPanel.AppendChild(backButtonElement);
                    stackPanel.InsertBefore(comment, backButtonElement);

                    if (lastPageNode == null || lastPageNode.Name.Contains(".Resources"))
                    {
                        root.AppendChild(stackPanel);
                    }
                    else
                    {
                        root.RemoveChild(lastPageNode);
                        stackPanel.AppendChild(lastPageNode);
                        root.AppendChild(stackPanel);
                    }

                    doc.Save(file);
                }
            }

            return new WindowsDesktopUpdaterResult(
               "UA302",
               RuleName: Id,
               FullDescription: Title,
               true,
               string.Empty,
               new List<string>());
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            await Task.Yield();

            var filesToUpdate = new List<string>();
            foreach (var project in inputs)
            {
                foreach (var file in project.FindFiles(".cs", ProjectItemType.Compile))
                {
                    var content = File.ReadAllText(file);
                    if (content.Contains("AppViewBackButtonVisibility"))
                    {
                        filesToUpdate.Add(file);
                    }
                }
            }

            return new WindowsDesktopUpdaterResult(
               Id,
               RuleName: Id,
               FullDescription: Title,
               filesToUpdate.Any(),
               AnalysisString,
               filesToUpdate);
        }
    }
}
