// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    public class PackageUpdater
    {
        private readonly XDocument _doc;
        private readonly ILogger _logger;
        //private readonly IProjectFile _file;
        private List<string> coreWCFPackages = new List<string>
        { "CoreWCF.NetTcp", "CoreWCF.Primitives", "CoreWCF.ConfigurationManager", "CoreWCF.Http", "CoreWCF.WebHttp" };

        public PackageUpdater(XDocument doc, ILogger logger)
        {
            _doc = doc;
            _logger = logger;
            //_file = file;
        }

        public XDocument UpdatePackages()
        {
            // removes references to System.ServiceModel
            _doc.Root.Descendants("PackageReference")
                .Where(x => x.Attribute("Include").Value.StartsWith("System.ServiceModel", System.StringComparison.OrdinalIgnoreCase))
                .Remove();
            _logger.LogDebug("Finish removing references to System.ServiceModel.");

            // adds CoreWCF packages
            var currPackages = from package in _doc.Root.Descendants("PackageReference")
                           where package.Attribute("Include").Value.StartsWith("CoreWCF", System.StringComparison.OrdinalIgnoreCase)
                           select package.Attribute("Include").Value;
            var corewcfPackages = XDocument.Parse(Constants.CoreWCFPackages).Root.Descendants();
            corewcfPackages.Where(x => currPackages.Contains(x.Attribute("Include").Value)).Remove();

            //List<NuGetReference> addPackages = new List<NuGetReference>();
            //foreach (var package in coreWCFPackages)
            //{
            //    if (!packages.Contains(package))
            //    {
            //        addPackages.Add(new NuGetReference(package, "1.1.0"));
            //    }
            //}

            _doc.Root.Descendants("PackageReference").Ancestors().First().Add(corewcfPackages);
            _logger.LogDebug("Finish adding references to CoreWCF packages.");
            return _doc;
        }

        public XDocument UpdateSDK()
        {
            _doc.Root.SetAttributeValue("Sdk", "Microsoft.NET.Sdk.Web");
            _logger.LogDebug("Finish setting Sdk to Sdk.Web.");
            return _doc;
        }
    }
}
