// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.Frameworks;

namespace Microsoft.DotNet.UpgradeAssistant.Cli.Commands.AnalyzeBinaries
{
    public class ConsoleAnalyzeBinariesCommandOptions : BaseUpgradeAssistantOptions, IBinaryAnalysisExecutorOptions
    {
        public ConsoleAnalyzeBinariesCommandOptions(IReadOnlyCollection<FileSystemInfo> filesOrDirectories)
        {
            this.Content = filesOrDirectories.Select(i => i.FullName).ToList();
        }

        public bool Obsoletion { get; set; }

        public bool AllowPrerelease { get; set; }

        public IReadOnlyCollection<Platform> Platform { get; set; } = Array.Empty<Platform>();

        public IReadOnlyCollection<string> Content { get; set; } = Array.Empty<string>();
    }
}
