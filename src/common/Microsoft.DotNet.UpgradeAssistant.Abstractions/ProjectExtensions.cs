// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class ProjectExtensions
    {
        public static IProject Required(this IProject? project)
            => project ?? throw new InvalidOperationException("Project cannot be null");
    }
}
