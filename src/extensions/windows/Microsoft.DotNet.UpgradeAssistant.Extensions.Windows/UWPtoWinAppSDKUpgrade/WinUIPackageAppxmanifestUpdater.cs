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
    internal class WinUIPackageAppxmanifestUpdater : IUpdater<IProject>
    {
        public const string RuleID = "UA304";

        public string Id => typeof(WinUIPackageAppxmanifestUpdater).FullName;

        public string Title => "Update package.appxmanifest";

        public string Description => "Update the package.appxmanifest file";

        private const string ErrorMessage = @"Unable to your fix package.appxmanifest file automatically.
                We suggest copying the package.appxmanifest file from a new Windows App SDK projecta and editing it as required.";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        private readonly ILogger _logger;

        public WinUIPackageAppxmanifestUpdater(ILogger<WinUIPackageAppxmanifestUpdater> logger)
        {
            _logger = logger;
        }

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            await Task.Yield();
#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                foreach (var project in inputs)
                {
                    var filePath = project.FindFiles("Package.appxmanifest").FirstOrDefault();
                    if (string.IsNullOrEmpty(filePath))
                    {
                        continue;
                    }

                    var doc = new XmlDocument();
                    doc.Load(filePath);

                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                    nsmgr.AddNamespace("def", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
                    nsmgr.AddNamespace("mp", "http://schemas.microsoft.com/appx/2014/phone/manifest");
                    nsmgr.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10");

                    var root = doc.DocumentElement;
                    var phoneIdentityElement = root.SelectSingleNode("mp:PhoneIdentity", nsmgr);
                    if (phoneIdentityElement != null)
                    {
                        root.RemoveChild(phoneIdentityElement);
                    }

                    var appElement = (XmlElement)root.SelectSingleNode("def:Applications/def:Application[@Id]", nsmgr);
                    appElement.SetAttribute("EntryPoint", "$targetentrypoint$");

                    var targetDevicesUniversalElement = (XmlElement)root.SelectSingleNode("def:Dependencies/def:TargetDeviceFamily[@Name='Windows.Universal']", nsmgr);

                    var targetFramework = context.CurrentProject!.TargetFrameworks.FirstOrDefault();
                    if (targetFramework != null && targetFramework.PlatformVersion != null)
                    {
                        var platformVersion = new Version(targetFramework.PlatformVersion.Major == -1 ? 10 : targetFramework.PlatformVersion.Major,
                            targetFramework.PlatformVersion.Minor == -1 ? 0 : targetFramework.PlatformVersion.Minor,
                            targetFramework.PlatformVersion.Build == -1 ? 19041 : targetFramework.PlatformVersion.Build,
                            targetFramework.PlatformVersion.Revision == -1 ? 0 : targetFramework.PlatformVersion.Revision);
                        targetDevicesUniversalElement.SetAttribute("MaxVersionTested", platformVersion!.ToString());
                    }

                    targetDevicesUniversalElement.SetAttribute("Name", "Windows.Desktop");
                    doc.Save(filePath);
                }
            }

            // Likely to fail while parsing xml if the format does not match the expected format.
            // Move on to the next step if it fails
            catch (Exception)
            {
                this._logger.LogError(ErrorMessage);
                return new WindowsDesktopUpdaterResult(
                  RuleID,
                  RuleName: Id,
                  FullDescription: Title,
                  false,
                  string.Empty,
                  new List<string>());
            }
#pragma warning restore CA1031 // Do not catch general exception types

            return new WindowsDesktopUpdaterResult(
               RuleID,
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
                var filePath = project.FindFiles("Package.appxmanifest").FirstOrDefault();
                if (string.IsNullOrEmpty(filePath))
                {
                    continue;
                }

                var doc = new XmlDocument();
                doc.Load(filePath);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("def", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");

                var root = doc.DocumentElement;
                var targetDevicesDesktopElement = (XmlElement)root.SelectSingleNode("def:Dependencies/def:TargetDeviceFamily[@Name='Windows.Desktop']", nsmgr);
                if (targetDevicesDesktopElement == null)
                {
                    filesToUpdate.Add(filePath);
                }
            }

            if (filesToUpdate.Any())
            {
                return new WindowsDesktopUpdaterResult(
                       RuleID,
                       RuleName: Id,
                       FullDescription: Title,
                       true,
                       "The package.appxmanifest file needs to be updated with the new Windows App SDK format",
                       filesToUpdate);
            }

            return new WindowsDesktopUpdaterResult(
               RuleID,
               RuleName: Id,
               FullDescription: Title,
               false,
               "The package.appxmanifest is up to date",
               ImmutableList<string>.Empty);
        }
    }
}
