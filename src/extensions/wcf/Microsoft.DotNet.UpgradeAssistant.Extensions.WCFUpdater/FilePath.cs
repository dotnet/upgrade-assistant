// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    public class FilePath
    {
        public string? Config { get; set; }

        public string? ProjectFile { get; set; }

        public string? MainFile { get; set; }

        public List<string>? DirectiveFiles { get; set; }
    }
}
