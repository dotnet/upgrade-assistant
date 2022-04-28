// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public const string NewBackButtonName = "UABackButton";
        public const string NewBackButtonClickMethodName = $"{NewBackButtonName}_Click";

        public string Id => "UA305";

        public string Title => "Insert back button in XAML";

        public string Description => "Adds a back button to all WinUI XAML pages.";

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
                        { "local", "using:PhotoLab" },
                        { "muxc", "using:Microsoft.UI.Xaml.Controls" },
                        { "d", "http://schemas.microsoft.com/expression/blend/2008" },
                        { "mc", "http://schemas.openxmlformats.org/markup-compatibility/2006" }
                    };
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                    foreach (var ns in xmlNamespaces)
                    {
                        nsmgr.AddNamespace(ns.Key, ns.Value);
                    }

                    var root = doc.DocumentElement;
                    if (root == null || root.Name != "Page")
                    {
                        continue;
                    }

                    foreach (var child in root.ChildNodes)
                    {
                        if (child.GetType() == typeof(XmlElement))
                        {
                            XmlElement element = (XmlElement)child;
                            if (element.Name == "Page.Resources")
                            {
                                continue;
                            }

                            var backButtonElement = doc.CreateElement("AppBarButton", element.NamespaceURI);
                            backButtonElement.SetAttribute("Name", xmlNamespaces["x"], NewBackButtonName);
                            backButtonElement.SetAttribute("Click", element.NamespaceURI, NewBackButtonClickMethodName);
                            backButtonElement.SetAttribute("Margin", element.NamespaceURI, "0,0,12,0");

                            var backButtonSymbol = doc.CreateElement("SymbolIcon", element.NamespaceURI);
                            backButtonSymbol.SetAttribute("Symbol", element.NamespaceURI, "Back");
                            backButtonElement.AppendChild(backButtonSymbol);

                            var comment = doc.CreateComment(@$"TODO {WinUIBackButtonAnalyzer.DiagnosticId} Default back button in the title bar does not exist in WinUI3 apps.
        We have created a custom back button for you. Feel free to rename and edit its position/behavior.
        Read: https://aka.ms/UWP.NetUpgrade/UA3015");
                            element.PrependChild(backButtonElement);
                            element.InsertBefore(comment, backButtonElement);
                            break;
                        }
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
            return new WindowsDesktopUpdaterResult(
               "UA302",
               RuleName: Id,
               FullDescription: Title,
               true,
               string.Empty,
               new List<string>());
        }
    }
}
