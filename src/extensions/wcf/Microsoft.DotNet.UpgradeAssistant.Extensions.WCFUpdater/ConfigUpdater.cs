// Licensed to the .NET Foundation under one or more agreements.
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
            SetServiceAndBehaviorNames();
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
                var unsupported_endpoint = from el in wcfConfig.Root.DescendantsAndSelf("endpoint")
                    where el.Attribute("binding").Value.StartsWith("mex", StringComparison.Ordinal)
                    select el;
                unsupported_endpoint.First().AddBeforeSelf(new XComment(Constants.MexEndpoint));
                unsupported_endpoint.Remove();
                _logger.LogWarning("The mex endpoint is removed from .config file, and service metadata behavior is configured in the source code instead.");
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
                var uri = GetSchemeToAddressMapping(service.Attribute("behaviorConfiguration").Value);

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
                        else if (endpoint.Attribute("binding").Value.IndexOf("Https", StringComparison.Ordinal) >= 0)
                        {
                            endpoint.Attribute("address").Value = Path.Combine(uri[Uri.UriSchemeHttps].PathAndQuery, ad).Replace("\\", "/");
                        }
                        else if (endpoint.Attribute("binding").Value.IndexOf("Http", StringComparison.Ordinal) >= 0)
                        {
                            endpoint.Attribute("address").Value = Path.Combine(uri[Uri.UriSchemeHttp].PathAndQuery, ad).Replace("\\", "/");
                        }
                    }
                }
            }

            _logger.LogDebug("Finished creating the new configuration file.");
            return config;
        }

        public MetadataType SupportsMetadataBehavior(string name)
        {
            var results = GetBehavior(name).DescendantsAndSelf("serviceMetadata");
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

        public Dictionary<string, string> SupportsServiceDebug(string name)
        {
            var results = GetBehavior(name).DescendantsAndSelf("serviceDebug").SingleOrDefault();
            var debug = new Dictionary<string, string>();
            foreach (var attribute in results.Attributes())
            {
                debug.Add(attribute.Name.ToString(), attribute.Value);
            }

            return debug;
        }

        public Dictionary<string, Uri> GetSchemeToAddressMapping(string behaviorName)
        {
            var uri = new Dictionary<string, Uri>();
            var service = GetService(behaviorName);

            // add base address from host element
            var baseAddress = from address in service.DescendantsAndSelf("add")
                              where address.Attribute("baseAddress").Value is not null
                              select address;
            foreach (var address in baseAddress)
            {
                var ad = new Uri(address.Attribute("baseAddress").Value);
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
            bool httpBinding = (from b in bindings where b.IndexOf("HttpBinding", StringComparison.Ordinal) >= 0 select b).Any();
            bool httpsBinding = (from b in bindings where b.IndexOf("HttpsBinding", StringComparison.Ordinal) >= 0 select b).Any();

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

        public Dictionary<string, string> GetServiceCredentials(string name)
        {
            var result = new Dictionary<string, string>();
            var behavior = GetBehavior(name);
            string[] names = { "clientCertificate", "serviceCertificate", "userNameAuthentication", "windowsAuthentication" };
            foreach (var n in names)
            {
                foreach (var c in behavior.DescendantsAndSelf(n))
                {
                    if (!c.HasAttributes)
                    {
                        foreach (var element in c.Elements())
                        {
                            foreach (var a in element.Attributes())
                            {
                                result.Add(n + "/" + a.Name, a.Value);
                            }
                        }
                    }

                    foreach (var a in c.Attributes())
                    {
                        result.Add(n + "/" + a.Name, a.Value);
                    }
                }
            }

            return result;
        }

        public bool UsesServiceCertificate(string name)
        {
            var cert = GetBehavior(name).DescendantsAndSelf("serviceCertificate");
            if (cert.Any())
            {
                return cert.First().Attribute("storeLocation") != null && cert.First().Attribute("storeName") != null &&
                    cert.First().Attribute("x509FindType") != null && cert.First().Attribute("findValue") != null;
            }

            return false;
        }

        // returns if the NetTcp binding is configured to use certificate
        public bool NetTcpUsesCertificate()
        {
            var netTcp = _config.Root.DescendantsAndSelf("netTcpBinding");
            if (netTcp.Any())
            {
                var security = netTcp.First().DescendantsAndSelf("security").Single();
                if (security is not null)
                {
                    if (security.Attribute("mode").Value.Equals("TransportWithMessageCredential", StringComparison.Ordinal))
                    {
                        return true;
                    }

                    if (security.Element("transport") is not null &&
                        security.Element("transport").Attribute("clientCredentialType").Value.Equals("Certificate", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool HasWindowsAuthentication()
        {
            string[] httpBindings = new string[] { "basicHttpBinding", "netHttpBinding", "webHttpBinding", "wsHttpBinding" };
            foreach (var httpBinding in httpBindings)
            {
                var binding = _config.Root.DescendantsAndSelf(httpBinding);
                if (binding.Any())
                {
                    var security = binding.First().DescendantsAndSelf("security").Single();
                    if (security is not null)
                    {
                        if (security.Attribute("mode").Value.Equals("Transport", StringComparison.OrdinalIgnoreCase) ||
                            security.Attribute("mode").Value.Equals("TransportCredentialOnly", StringComparison.OrdinalIgnoreCase))
                        {
                            var transport = security.Element("transport");
                            if (transport.Attribute("clientCredentialType") is not null &&
                                (transport.Attribute("clientCredentialType").Value.Equals("Windows", StringComparison.OrdinalIgnoreCase) ||
                                transport.Attribute("clientCredentialType").Value.Equals("Ntlm", StringComparison.OrdinalIgnoreCase)))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public HashSet<string> GetBindings(string behaviorName)
        {
            var bindings = new HashSet<string>();
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

        private void SetServiceAndBehaviorNames()
        {
            foreach (var s in _config.Root.DescendantsAndSelf("service"))
            {
                if (s.Attribute("behaviorConfiguration") == null)
                {
                    s.SetAttributeValue("behaviorConfiguration", string.Empty);
                    _logger.LogWarning("Set the attribute service/behaviorConfiguration value to empty string to avoid null.");
                }
            }

            foreach (var s in _config.Root.DescendantsAndSelf("serviceBehaviors").DescendantsAndSelf("behavior"))
            {
                if (s.Attribute("name") == null)
                {
                    s.SetAttributeValue("name", string.Empty);
                    _logger.LogWarning("Set the attribute behavior/name value to empty string to avoid null.");
                }
            }
        }
    }
}
