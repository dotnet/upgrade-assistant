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
    [Flags]
    public enum MetadataType
    {
        Http = 1,
        Https = 2,
        Both = 3,
        None = 0
    }

    public class ConfigUpdater
    {
        private readonly XDocument _config;
        private readonly ILogger<ConfigUpdater> _logger;

        public ConfigUpdater(XDocument doc, ILogger<ConfigUpdater> logger)
        {
            _config = doc;
            _logger = logger;
        }

        // Updates the original config file by removing the system.serviceModel element
        public XDocument UpdateOldConfig()
        {
            var oldConfig = new XDocument(_config);
            var serviceModel = oldConfig.Root.DescendantsAndSelf("system.serviceModel");
            serviceModel.Single().AddBeforeSelf(new XComment(Constants.ServiceModelComment));
            serviceModel.Remove();

            _logger.LogDebug("The original config file finished updating. System.serviceModel element was removed.");
            return oldConfig;
        }

        public XDocument GenerateNewConfig()
        {
            var wcfConfig = new XDocument(new XElement("configuration", _config.Root.DescendantsAndSelf("system.serviceModel")));

            // comment out host and behavior elements which are not supported by CoreWCF and configured in the code instead
            var baseAddress = wcfConfig.Root.DescendantsAndSelf("host");
            var serviceBehavior = wcfConfig.Root.DescendantsAndSelf("behaviors");
            if (baseAddress.Any())
            {
                var host = baseAddress.First();
                host.AddBeforeSelf(new XComment(Constants.HostComment));
                host.ReplaceWith(new XComment(host.ToString()));
            }

            if (serviceBehavior.Any())
            {
                var behaviors = serviceBehavior.Single();
                behaviors.AddBeforeSelf(new XComment(Constants.BehaviorComment));
                behaviors.ReplaceWith(new XComment(behaviors.ToString()));
            }

            // find and remove the mex endpoint
            if (IncludesMexEndpoint())
            {
                var unsupported_endpoint = from el in wcfConfig.Root.DescendantsAndSelf("endpoint")
                                           where el.Attribute("binding").Value.StartsWith("mex", StringComparison.Ordinal)
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
            var uri = GetSchemeToAddressMapping();

            // add path from base address to endpoints
            var endpoints = config.Root.DescendantsAndSelf("endpoint");
            foreach (var endpoint in endpoints)
            {
                var ad = endpoint.Attribute("address").Value;
                if (endpoint.Attribute("binding").Value.StartsWith("netTcp", StringComparison.Ordinal))
                {
                    endpoint.Attribute("address").Value = Path.Combine(uri[Uri.UriSchemeNetTcp].PathAndQuery, ad).Replace("\\", "/");
                }
                else if (endpoint.Attribute("binding").Value.IndexOf("Https", StringComparison.Ordinal) >= 0)
                {
                    endpoint.Attribute("address").Value = Path.Combine(uri[Uri.UriSchemeHttps].PathAndQuery, ad).Replace("\\", "/");
                }
                else if (endpoint.Attribute("binding").Value.IndexOf("Http", StringComparison.Ordinal) >= 0)
                {
                    endpoint.Attribute("address").Value = Path.Combine(uri[Uri.UriSchemeHttp].PathAndQuery, ad).Replace("\\", "/");
                }
            }

            _logger.LogDebug("Finished creating the new configuration file.");
            return config;
        }

        public MetadataType SupportsMetadataBehavior()
        {
            var results = _config.Root.DescendantsAndSelf("serviceMetadata");

            if (results.Any())
            {
                var el = results.Single();
                var http = el.Attribute("httpGetEnabled") is not null && el.Attribute("httpGetEnabled").Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                var https = el.Attribute("httpsGetEnabled") is not null && el.Attribute("httpsGetEnabled").Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                if (http && https)
                {
                    return MetadataType.Both;
                }
                else if (http)
                {
                    return MetadataType.Http;
                }
                else if (https)
                {
                    return MetadataType.Https;
                }
            }

            return MetadataType.None;
        }

        public bool IncludesMexEndpoint()
        {
            foreach (var el in _config.Root.DescendantsAndSelf("endpoint"))
            {
                if (el.Attribute("address").Value.Equals("mex", StringComparison.Ordinal))
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
                var el = results.Single();
                if (el.Attribute("includeExceptionDetailInFaults") is not null && el.Attribute("includeExceptionDetailInFaults").Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public Dictionary<string, Uri> GetSchemeToAddressMapping()
        {
            var uri = new Dictionary<string, Uri>();
            var baseAddress =
                from address in _config.Root.DescendantsAndSelf("add")
                where address.Attribute("baseAddress").Value is not null
                select address;
            foreach (var address in baseAddress)
            {
                var ad = new Uri(address.Attribute("baseAddress").Value);
                uri.Add(ad.Scheme, ad);
            }

            return uri;
        }

        public HashSet<string> GetBindings()
        {
            var bindings = new HashSet<string>();
            var endpoints = _config.Root.DescendantsAndSelf("endpoint");
            foreach (var endpoint in endpoints)
            {
                bindings.Add(endpoint.Attribute("binding").Value);
            }

            return bindings;
        }
    }
}
