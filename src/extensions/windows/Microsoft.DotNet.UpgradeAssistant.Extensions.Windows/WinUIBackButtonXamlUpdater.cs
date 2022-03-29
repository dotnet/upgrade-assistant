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
        public string Id => "UA305";

        public string Title => "WinUI Xaml Back button inserter";

        public string Description => "Adds a back button to all WinUI XAML pages.";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        public WinUIBackButtonXamlUpdater(ILogger<WinUIBackButtonXamlUpdater> logger)
        {

        }

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
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
                            backButtonElement.SetAttribute("Name", xmlNamespaces["x"], "BackButton");
                            backButtonElement.SetAttribute("Click", element.NamespaceURI, "BackButton_Click");
                            backButtonElement.SetAttribute("Margin", element.NamespaceURI, "0,0,12,0");

                            var backButtonSymbol = doc.CreateElement("SymbolIcon", element.NamespaceURI);
                            backButtonSymbol.SetAttribute("Symbol", element.NamespaceURI, "Back");
                            backButtonElement.AppendChild(backButtonSymbol);

                            element.PrependChild(backButtonElement);
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
               "",
               new List<string>());
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            return new WindowsDesktopUpdaterResult(
               "UA302",
               RuleName: Id,
               FullDescription: Title,
               true,
               "",
               new List<string>());
        }
    }
}
