// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class Defaults
    {
        public static MigrateOptions DefaultMigrateOptions =>
            new()
            {
                // Migrate options are only valid if their Project property points at a file that exists
                Project = new FileInfo(typeof(Defaults).Assembly.Location)
            };
    }
}
