// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    internal class WinUINamespaceUpdater : IUpdater<IProject>
    {
        private const string RuleId = "UA301";

        public string Id => typeof(WinUINamespaceUpdater).FullName;

        public string Title => "Update WinUI namespaces";

        public string Description => "Update the namespace for APIs that live in different namespaces in Windows App SDK";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        private readonly ILogger<WinUINamespaceUpdater> _logger;

        private readonly Dictionary<string, string>? _namespaceUpdates;

        public WinUINamespaceUpdater(ILogger<WinUINamespaceUpdater> logger, IOptions<WinUIOptions> options)
        {
            this._logger = logger;
            this._namespaceUpdates = options.Value.NamespaceUpdates;
        }

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            if (_namespaceUpdates == null)
            {
                return new WindowsDesktopUpdaterResult(
                    RuleId, RuleName: Id, FullDescription: Title, false, "", new List<string>());
            }

            foreach (var project in inputs)
            {
                foreach (var itemPath in project.FindFiles(".cs", ProjectItemType.Compile))
                {
                    var contents = new StringBuilder(File.ReadAllText(itemPath));
                    foreach (var nameplaceReplace in _namespaceUpdates)
                    {
                        contents.Replace(nameplaceReplace.Key, nameplaceReplace.Value);
                    }

                    File.WriteAllText(itemPath, contents.ToString());
                }
            }

            return new WindowsDesktopUpdaterResult(
                RuleId,
                RuleName: Id,
                FullDescription: Title,
                true,
                "",
                new List<string>());
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            if (_namespaceUpdates == null)
            {
                return new WindowsDesktopUpdaterResult(
                    RuleId,
                    RuleName: Id,
                    FullDescription: Title,
                    false,
                    "",
                    new List<string>());
            }

            return new WindowsDesktopUpdaterResult(
                RuleId,
                RuleName: Id,
                FullDescription: Title,
                true,
                "",
                new List<string>());
        }
    }
}
