// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    public class ConfigUpdater
    {
        private readonly XDocument _config;
        private readonly ILogger _logger;

        public ConfigUpdater(XDocument doc, ILogger logger)
        {
            _config = doc;
            _logger = logger;
        }

        // Updates the original config file by removing the system.serviceModel element
        public XDocument UpdateOldConfig()
        {
            XDocument oldConfig = new XDocument(_config);
            var serviceModel = oldConfig.Root.DescendantsAndSelf("system.serviceModel");
            serviceModel.First().AddBeforeSelf(new XComment(Constants.ServiceModelComment));
            serviceModel.Remove();

            _logger.LogDebug("The original config file finished updating. System.serviceModel element was removed.");
            return oldConfig;
        }

        public XDocument GenerateNewConfig()
        {
            XDocument wcfConfig = new XDocument(new XElement("configuration", _config.Root.DescendantsAndSelf("system.serviceModel")));

            // comment out host and behavior elements which are not supported by CoreWCF and configured in the code instead
            var baseAddress = wcfConfig.Root.DescendantsAndSelf("host");
            var serviceBehavior = wcfConfig.Root.DescendantsAndSelf("behaviors");
            if (baseAddress.Any())
            {
                baseAddress.First().AddBeforeSelf(new XComment(Constants.HostComment));
                baseAddress.First().ReplaceWith(new XComment(baseAddress.First().ToString()));
            }

            if (serviceBehavior.Any())
            {
                serviceBehavior.First().AddBeforeSelf(new XComment(Constants.BehaviorComment));
                serviceBehavior.First().ReplaceWith(new XComment(serviceBehavior.First().ToString()));
            }

            // find and remove the mex endpoint
            if (IncludesMexEndpoint())
            {
                IEnumerable<XElement> unsupported_endpoint =
                    from el in wcfConfig.Root.DescendantsAndSelf("endpoint")
                    where el.Attribute("binding").Value.Contains("mex")
                    select el;
                unsupported_endpoint.First().AddBeforeSelf(new XComment(Constants.MexEndpoint));
                unsupported_endpoint.Remove();
                _logger.LogWarning("The mex endpoint is removed from .config and service metadata behavior is configured in the source code instead.");
            }

            wcfConfig = UpdateEndpoints(wcfConfig);
            _logger.LogDebug("Finished creating the new configuration file.");
            return wcfConfig;
        }

        // Adds path from base address to the beginning of endpoints if needed
        private XDocument UpdateEndpoints(XDocument config)
        {
            // get base address
            var uri = GetUri();

            // add path from base address to endpoints
            var endpoints = config.Root.DescendantsAndSelf("endpoint");
            foreach (var endpoint in endpoints)
            {
                var ad = endpoint.Attribute("address").Value;
                if (endpoint.Attribute("binding").Value.Contains("netTcp"))
                {
                    endpoint.Attribute("address").Value = Path.Combine(uri[Uri.UriSchemeNetTcp].PathAndQuery, ad).Replace("\\", "/");
                }
                else if (endpoint.Attribute("binding").Value.Contains("Https"))
                {
                    endpoint.Attribute("address").Value = Path.Combine(uri[Uri.UriSchemeHttps].PathAndQuery, ad).Replace("\\", "/");
                }
                else if (endpoint.Attribute("binding").Value.Contains("Http"))
                {
                    endpoint.Attribute("address").Value = Path.Combine(uri[Uri.UriSchemeHttp].PathAndQuery, ad).Replace("\\", "/");
                }
            }

            _logger.LogDebug("Finished creating the new configuration file.");
            return config;
        }

        // Returns 0 if metadata is not supported, 1 if it's  supported with http, 2 with https, 3 with both http and https</returns>
        public int SupportsMetadataBehavior()
        {
            var results = _config.Root.DescendantsAndSelf("serviceMetadata");
            if (results.Any())
            {
                var el = results.First();
                var http = el.Attribute("httpGetEnabled") != null && string.Equals(el.Attribute("httpGetEnabled").Value, "true", StringComparison.OrdinalIgnoreCase);
                var https = el.Attribute("httpsGetEnabled") != null && string.Equals(el.Attribute("httpsGetEnabled").Value, "true", StringComparison.OrdinalIgnoreCase);
                if (http && https)
                {
                    return 3;
                }
                else if (http)
                {
                    return 1;
                }
                else if (https)
                {
                    return 2;
                }
            }

            return 0;
        }

        public bool IncludesMexEndpoint()
        {
            foreach (XElement el in _config.Root.DescendantsAndSelf("endpoint"))
            {
                if (el.Attribute("address").Value == "mex")
                {
                    return true;
                }
            }

            return false;
        }

        public bool SupportsServiceDebug()
        {
            var results = _config.Root.DescendantsAndSelf("serviceDebug");
            if (results.Any())
            {
                var el = results.First();
                if (el.Attribute("includeExceptionDetailInFaults") != null && string.Equals(el.Attribute("includeExceptionDetailInFaults").Value, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public Dictionary<string, Uri> GetUri()
        {
            Dictionary<string, Uri> uri = new Dictionary<string, Uri>();
            var baseAddress =
                from address in _config.Root.DescendantsAndSelf("add")
                where address.Attribute("baseAddress").Value != null
                select address;
            foreach (var address in baseAddress)
            {
                Uri ad = new Uri(address.Attribute("baseAddress").Value);
                uri.Add(ad.Scheme, ad);
            }

            return uri;
        }

        public HashSet<string> GetBindings()
        {
            HashSet<string> bindings = new HashSet<string>();
            var endpoints = _config.Root.DescendantsAndSelf("endpoint");
            foreach (var endpoint in endpoints)
            {
                bindings.Add(endpoint.Attribute("binding").Value);
            }

            return bindings;
        }
    }
}
