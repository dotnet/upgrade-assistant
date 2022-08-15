﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
            SetServiceAndBehaviorNames(_logger);
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
                    where el.Attribute("binding").Value.StartsWith("mex", StringComparison.Ordinal)
                    select el;
                unsupported_endpoint.First().AddBeforeSelf(new XComment(Constants.MexEndpoint));
                unsupported_endpoint.Remove();
                _logger.LogWarning("The mex endpoint is removed from .config file and service metadata behavior is configured in the source code instead.");
            }

            wcfConfig = UpdateEndpoints(wcfConfig);
            _logger.LogDebug("Finished creating the new configuration file.");
            return wcfConfig;
        }

        // Adds path from base address to the beginning of endpoints if needed
        private XDocument UpdateEndpoints(XDocument config)
        {
            foreach (var service in config.Root.DescendantsAndSelf("service"))
            {
                var uri = GetUri(service.Attribute("behaviorConfiguration").Value);

                // add relative path from base address to endpoints
                foreach (var endpoint in service.DescendantsAndSelf("endpoint"))
                {
                    var ad = endpoint.Attribute("address").Value;
                    if (Uri.IsWellFormedUriString(ad, UriKind.Relative))
                    {
                        if (endpoint.Attribute("binding").Value.StartsWith("netTcp", StringComparison.Ordinal))
                        {
                            endpoint.Attribute("address").Value = Path.Combine(uri[Uri.UriSchemeNetTcp].PathAndQuery, ad).Replace("\\", "/");
                        }
                        else if (endpoint.Attribute("binding").Value.Contains("Https", StringComparison.Ordinal))
                        {
                            endpoint.Attribute("address").Value = Path.Combine(uri[Uri.UriSchemeHttps].PathAndQuery, ad).Replace("\\", "/");
                        }
                        else if (endpoint.Attribute("binding").Value.Contains("Http", StringComparison.Ordinal))
                        {
                            endpoint.Attribute("address").Value = Path.Combine(uri[Uri.UriSchemeHttp].PathAndQuery, ad).Replace("\\", "/");
                        }
                    }
                }
            }

            _logger.LogDebug("Finished creating the new configuration file.");
            return config;
        }

        // Returns 0 if metadata is not supported, 1 if it's  supported with http, 2 with https, 3 with both http and https</returns>
        public int SupportsMetadataBehavior(string name)
        {
            var results = GetBehavior(name).DescendantsAndSelf("serviceMetadata");
            if (results.Any())
            {
                var el = results.First();
                var http = el.Attribute("httpGetEnabled") != null && el.Attribute("httpGetEnabled").Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                var https = el.Attribute("httpsGetEnabled") != null && el.Attribute("httpsGetEnabled").Value.Equals("true", StringComparison.OrdinalIgnoreCase);
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
                if (el.Attribute("address").Value.Equals("mex", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool SupportsServiceDebug(string name)
        {
            var results = GetBehavior(name).DescendantsAndSelf("serviceDebug");
            if (results.Any())
            {
                var el = results.First();
                if (el.Attribute("includeExceptionDetailInFaults") != null && el.Attribute("includeExceptionDetailInFaults").Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public Dictionary<string, Uri> GetUri(string behaviorName)
        {
            Dictionary<string, Uri> uri = new Dictionary<string, Uri>();
            var service = GetService(behaviorName);

            // add base address from host element
            var baseAddress = from address in service.DescendantsAndSelf("add")
                              where address.Attribute("baseAddress").Value != null
                              select address;
            foreach (var address in baseAddress)
            {
                Uri ad = new Uri(address.Attribute("baseAddress").Value);
                uri.Add(ad.Scheme, ad);
            }

            // add absolute uri from endpoint address
            foreach (var endpoint in service.DescendantsAndSelf("endpoint"))
            {
                var ad = endpoint.Attribute("address").Value;
                if (Uri.IsWellFormedUriString(ad, UriKind.Absolute))
                {
                    var value = new Uri(ad);
                    uri.Add(value.Scheme, value);
                }
            }

            var bindings = GetBindings(behaviorName);
            bool httpBinding = (from b in bindings where b.Contains("HttpBinding", StringComparison.Ordinal) select b).Any();
            bool httpsBinding = (from b in bindings where b.Contains("HttpsBinding", StringComparison.Ordinal) select b).Any();

            // adds default address if binding exists but no specific base address
            if (!uri.ContainsKey(Uri.UriSchemeNetTcp) && bindings.Contains("NetTcpBinding"))
            {
                uri.Add(Uri.UriSchemeNetTcp, new Uri("http://localhost:808"));
            }

            if (!uri.ContainsKey(Uri.UriSchemeHttp) && httpBinding)
            {
                uri.Add(Uri.UriSchemeHttp, new Uri("http://localhost:80"));
            }

            if (!uri.ContainsKey(Uri.UriSchemeHttps) && httpsBinding)
            {
                uri.Add(Uri.UriSchemeHttps, new Uri("http://localhost:443"));
            }

            return uri;
        }

        public HashSet<string> GetBindings(string behaviorName)
        {
            HashSet<string> bindings = new HashSet<string>();
            var endpoints = GetService(behaviorName).DescendantsAndSelf("endpoint");
            foreach (var endpoint in endpoints)
            {
                bindings.Add(endpoint.Attribute("binding").Value);
            }

            return bindings;
        }

        public HashSet<string> GetAllBehaviorNames()
        {
            HashSet<string> behaviors = new HashSet<string>();
            var behavior = _config.Root.DescendantsAndSelf("serviceBehaviors").DescendantsAndSelf("behavior");
            foreach (var b in behavior)
            {
                // assumes that the default name is string.empty
                behaviors.Add(b.Attribute("name").Value);
            }

            return behaviors;
        }

        public Dictionary<string, string> GetServiceBehaviorPair()
        {
            Dictionary<string, string> pair = new Dictionary<string, string>();
            foreach (var s in _config.Root.DescendantsAndSelf("service"))
            {
                pair.Add(s.Attribute("name").Value, s.Attribute("behaviorConfiguration").Value);
            }

            return pair;
        }

        private XElement GetBehavior(string name)
        {
            return (from b in _config.Root.DescendantsAndSelf("serviceBehaviors").DescendantsAndSelf("behavior")
                    where b.Attribute("name").Value.Equals(name, StringComparison.Ordinal)
                    select b).First();
        }

        private XElement GetService(string behaviorName)
        {
            return (from s in _config.Root.DescendantsAndSelf("service")
                    where s.Attribute("behaviorConfiguration").Value.Equals(behaviorName, StringComparison.Ordinal)
                    select s).First();
        }

        private void SetServiceAndBehaviorNames(ILogger logger)
        {
            foreach (var s in _config.Root.DescendantsAndSelf("service"))
            {
                if (s.Attribute("behaviorConfiguration") == null)
                {
                    s.SetAttributeValue("behaviorConfiguration", string.Empty);
                    logger.LogWarning("Set the attribute service/behaviorConfiguration value to empty string to avoid null.");
                }
            }

            foreach (var s in _config.Root.DescendantsAndSelf("serviceBehaviors").DescendantsAndSelf("behavior"))
            {
                if (s.Attribute("name") == null)
                {
                    s.SetAttributeValue("name", string.Empty);
                    logger.LogWarning("Set the attribute behavior/name value to empty string to avoid null.");
                }
            }
        }
    }
}
