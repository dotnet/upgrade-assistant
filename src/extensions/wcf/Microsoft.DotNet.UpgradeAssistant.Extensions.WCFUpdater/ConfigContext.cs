// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    public class ConfigContext
    {

        public Dictionary<string, Uri> SchemeToAddressMapping { get; }

        public ConfigContext(ConfigUpdater configUpdater, string name)
        {
            SchemeToAddressMapping = configUpdater.GetSchemeToAddressMapping(name);

        }
    }
}
