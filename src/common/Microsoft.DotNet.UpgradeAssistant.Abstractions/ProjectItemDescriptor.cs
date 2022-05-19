// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record ProjectItemDescriptor
    {
        public ProjectItemDescriptor(ProjectItemType itemType)
        {
            ItemType = itemType;
        }

        public ProjectItemType ItemType { get; init; }

        public string? Include { get; init; }

        public string? Exclude { get; init; }

        public string? Remove { get; init; }
    }
}
