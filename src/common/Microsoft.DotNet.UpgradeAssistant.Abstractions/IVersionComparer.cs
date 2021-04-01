// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IVersionComparer : IComparer<string>
    {
        bool IsMajorChange(string x, string y);
    }
}
