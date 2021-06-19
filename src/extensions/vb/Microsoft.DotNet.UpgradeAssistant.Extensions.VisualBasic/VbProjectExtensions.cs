// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.VisualBasic
{
    public static class VbProjectExtensions
    {
        public static bool IsVbClassLibrary(this IProject project)
        {
            if (project is null)
            {
                return false;
            }

            return project.Language == Language.VisualBasic && project.OutputType == ProjectOutputType.Library;
        }
    }
}
