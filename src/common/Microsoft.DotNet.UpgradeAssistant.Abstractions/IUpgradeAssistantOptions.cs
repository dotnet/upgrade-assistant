// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IUpgradeAssistantOptions
    {
        bool IsVerbose { get; }

        string? Format { get; }

        UpgradeTarget TargetTfmSupport { get; }
    }

    public class BaseUpgradeAssistantOptions : IUpgradeAssistantOptions
    {
        public bool Verbose { get; set; }

        public bool IsVerbose => Verbose;

        public string? Format { get; set; }

        public UpgradeTarget TargetTfmSupport { get; set; } = UpgradeTarget.STS;
    }
}
