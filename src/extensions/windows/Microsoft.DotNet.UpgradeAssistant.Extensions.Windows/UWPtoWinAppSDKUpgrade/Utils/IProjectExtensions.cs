// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade.Utils
{
    public static class IProjectExtensions
    {
        public static IEnumerable<string> AllProjectReferences(this IProject project) => project.GetRoslynProject().AllProjectReferences.Select(projRef => projRef.ProjectId.ToString());
    }
}
