// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    public class EntrypointResolver : IEntrypointResolver
    {
        public IEnumerable<IProject> GetEntrypoints(IEnumerable<IProject> projects, IReadOnlyCollection<string> names)
        {
            if (projects is null)
            {
                throw new ArgumentNullException(nameof(projects));
            }

            if (names is null)
            {
                throw new ArgumentNullException(nameof(names));
            }

            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);

            matcher.AddIncludePatterns(names);

            foreach (var project in projects)
            {
                if (matcher.Match(project.FileInfo.Name).HasMatches)
                {
                    yield return project;
                }
            }
        }
    }
}
