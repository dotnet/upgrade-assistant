// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade.Abstractions
{
    internal class ApiUpgrade
    {
        public ApiUpgrade(IApiDescription fromApi, IApiDescription toApi, bool needsManualUpgradation, string documentationUrl)
        {
            FromApi = fromApi;
            ToApi = toApi;
            NeedsManualUpgradation = needsManualUpgradation;
            DocumentationUrl = documentationUrl;
        }

        public IApiDescription FromApi { get; init; }

        public IApiDescription ToApi { get; init; }

        public string DocumentationUrl { get; init; }

        public bool NeedsManualUpgradation { get; init; }
    }
}
