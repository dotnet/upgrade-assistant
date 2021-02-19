// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Configuration
{
    public class ConfigFile
    {
        public ConfigFile(string path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Contents = XDocument.Load(path, LoadOptions.SetLineInfo);
        }

        public string Path { get; set; }

        public XDocument Contents { get; set; }
    }
}
