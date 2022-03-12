// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    internal class WinUIUnnecessaryFilesUpdater : IUpdater<IProject>
    {
        public string Id => "UA304";

        public string Title => "WinUI unnecessary files removal";

        public string Description => "Removes unnecessary files not required for WinUI";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            foreach (var project in inputs)
            {
                var filesToDelete = new List<string>();
                filesToDelete.AddRange(project.FindFiles("AssemblyInfo.cs"));
                filesToDelete.AddRange(project.FindFiles("App.xaml.old.cs"));
                filesToDelete.AddRange(project.FindFiles("App.old.xaml"));
                foreach (var file in filesToDelete)
                {
                    if (file != null)
                    {
                        File.Delete(file);
                    }
                }
            }

            return new WinformsUpdaterResult(
            "UA302",
            RuleName: Id,
            FullDescription: Title,
            true,
            "",
            new List<string>());
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            return new WinformsUpdaterResult(
               "UA302",
               RuleName: Id,
               FullDescription: Title,
               true,
               "",
               new List<string>());
        }
    }
}
