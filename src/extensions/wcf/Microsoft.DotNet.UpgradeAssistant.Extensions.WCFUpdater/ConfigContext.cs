// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    public class ConfigContext
    {
        public Dictionary<string, ServiceSpecificContext> ServiceContext { get; }

        public bool NetTcpUsesCertificate { get; }

        public bool HasMetadata { get; }

        public bool HasWindowsAuthentication { get; }

        public class ServiceSpecificContext
        {
            public Dictionary<string, Uri> SchemeToAddressMapping { get; }

            public MetadataType MetadataType { get; }

            public Dictionary<string, string> ServiceDebug { get; }

            public HashSet<string> BindingTypes { get; }

            public bool UsesServiceCertificate { get; }

            public Dictionary<string, string> ServiceCredentialsProperties { get; }

            public ServiceSpecificContext(ConfigUpdater configUpdater, string behaviorName)
            {
                SchemeToAddressMapping = configUpdater.GetSchemeToAddressMapping(behaviorName);
                MetadataType = configUpdater.SupportsMetadataBehavior(behaviorName);
                ServiceDebug = configUpdater.SupportsServiceDebug(behaviorName);
                BindingTypes = configUpdater.GetBindings(behaviorName);
                UsesServiceCertificate = configUpdater.UsesServiceCertificate(behaviorName);
                ServiceCredentialsProperties = configUpdater.GetServiceCredentials(behaviorName);
            }
        }

        public ConfigContext(ConfigUpdater configUpdater)
        {
            NetTcpUsesCertificate = configUpdater.NetTcpUsesCertificate();
            HasWindowsAuthentication = configUpdater.HasWindowsAuthentication();
            HasMetadata = false;

            var pair = configUpdater.GetServiceBehaviorPair();
            ServiceContext = new Dictionary<string, ServiceSpecificContext>();
            foreach (var serviceName in pair.Keys)
            {
                ServiceContext.Add(serviceName, new ServiceSpecificContext(configUpdater, pair[serviceName]));
                if (ServiceContext[serviceName].MetadataType != MetadataType.None)
                {
                    HasMetadata = true;
                }
            }
        }
    }
}
