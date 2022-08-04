// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    public class PackageUpdater
    {
        private readonly XDocument _doc;
        private readonly ILogger _logger;

        public PackageUpdater(XDocument doc, ILogger logger)
        {
            _doc = doc;
            _logger = logger;
        }

        public XDocument UpdatePackages()
        {
            // removes references to System.ServiceModel
            _doc.Root.Descendants("PackageReference")
                .Where(x => x.Attribute("Include").Value.StartsWith("System.ServiceModel", System.StringComparison.OrdinalIgnoreCase))
                .Remove();
            _logger.LogDebug("Finish removing references to System.ServiceModel.");

            // adds CoreWCF packages
            var reference_group = _doc.Root.Descendants("PackageReference").Ancestors().First();
            var corewcfPackages = XDocument.Parse(Constants.CoreWCFPackages);
            reference_group.Add(corewcfPackages.Root.Descendants());
            _logger.LogDebug("Finish adding references to CoreWCF.");
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
