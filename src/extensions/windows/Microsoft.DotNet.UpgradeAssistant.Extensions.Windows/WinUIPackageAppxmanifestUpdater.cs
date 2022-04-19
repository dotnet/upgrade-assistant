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

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    internal class WinUIPackageAppxmanifestUpdater : IUpdater<IProject>
    {
        public const string RuleID = "UA304";

        public string Id => typeof(WinUIPackageAppxmanifestUpdater).FullName;

        public string Title => "Update package.appxmanifest";

        public string Description => "Update the package.appxmanifest file";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            foreach (var project in inputs)
            {
                var file = project.FindFiles("Package.appxmanifest").First();
                if (file == null)
                {
                    continue;
                }

                var doc = new XmlDocument();
                doc.Load(file);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("def", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
                nsmgr.AddNamespace("mp", "http://schemas.microsoft.com/appx/2014/phone/manifest");
                nsmgr.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10");

                var root = doc.DocumentElement;
                root.RemoveChild(root.SelectSingleNode("mp:PhoneIdentity", nsmgr));

                var appElement = (XmlElement)root.SelectSingleNode("def:Applications/def:Application[@Id='App']", nsmgr);
                appElement.SetAttribute("EntryPoint", "$targetentrypoint$");

                var targetDevicesUniversalElement = (XmlElement)root.SelectSingleNode("def:Dependencies/def:TargetDeviceFamily[@Name='Windows.Universal']", nsmgr);
                targetDevicesUniversalElement.SetAttribute("MinVersion", "10.0.17763.0");
                targetDevicesUniversalElement.SetAttribute("MaxVersionTested", "10.0.19041.0");

                var targetDeviceDesktopElement = doc.CreateElement("TargetDeviceFamily", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
                targetDeviceDesktopElement.SetAttribute("Name", "Windows.Desktop");
                targetDeviceDesktopElement.SetAttribute("MinVersion", "10.0.17763.0");
                targetDeviceDesktopElement.SetAttribute("MaxVersionTested", "10.0.19041.0");
                targetDeviceDesktopElement.RemoveAttribute("xmlns");
                root.SelectSingleNode("def:Dependencies", nsmgr).AppendChild(targetDeviceDesktopElement);

                doc.Save(file);
            }

            return new WindowsDesktopUpdaterResult(
               RuleID,
               RuleName: Id,
               FullDescription: Title,
               true,
               "",
               new List<string>());
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            return new WindowsDesktopUpdaterResult(
               RuleID,
               RuleName: Id,
               FullDescription: Title,
               true,
               "",
               new List<string>());
        }
    }
}
