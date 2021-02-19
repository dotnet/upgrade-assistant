// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.DotNet.UpgradeAssistant.Steps.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.ConfigUpdaters
{
    public class UnsupportedSectionConfigUpdater : IConfigUpdater
    {
        private static (string Name, string Issue)[] _names = new (string, string)[]
        {
            ("system.diagnostics", "https://github.com/dotnet/runtime/issues/23937")
        };

        private readonly ILogger<UnsupportedSectionConfigUpdater> _logger;
        private readonly XmlWriterSettings _settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "    ",
        };

        public string Id => typeof(UnsupportedSectionConfigUpdater).FullName!;

        public string Title => "Disable unsupported configuration sections";

        public string Description => "Some sections are not supported in .NET Core. Often, these must be configured programmatically. This step will comment them out, which may change runtime behavior.";

        public BuildBreakRisk Risk => BuildBreakRisk.Low;

        public UnsupportedSectionConfigUpdater(ILogger<UnsupportedSectionConfigUpdater> logger)
        {
            _logger = logger;
        }

        public Task<bool> ApplyAsync(IMigrationContext context, ImmutableArray<ConfigFile> configFiles, CancellationToken token)
        {
            var applied = false;

            foreach (var configFile in configFiles)
            {
                var updated = false;

                foreach (var (section, issue) in GetUnsupportedSections(configFile))
                {
                    section.ReplaceWith(
                        new XComment($" {section.Name} section is not supported on .NET 5 (see {issue})"),
                        new XComment(section.ToString()));
                    updated = true;
                }

                if (updated)
                {
                    applied = true;

                    using var writer = XmlWriter.Create(configFile.Path, _settings);
                    configFile.Contents.WriteTo(writer);

                    _logger.LogInformation("Configuration file {Path} has been updated", configFile.Path);
                }
            }

            return Task.FromResult(applied);
        }

        public Task<bool> IsApplicableAsync(IMigrationContext context, ImmutableArray<ConfigFile> configFiles, CancellationToken token)
        {
            foreach (var configFile in configFiles)
            {
                if (GetUnsupportedSections(configFile).Any())
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        private IEnumerable<(XElement Section, string Issue)> GetUnsupportedSections(ConfigFile file)
        {
            var configuration = file.Contents.Element("configuration");

            if (configuration is null)
            {
                yield break;
            }

            foreach (var (name, issue) in _names)
            {
                var section = configuration.Element(name);

                if (section is not null)
                {
                    _logger.LogInformation("{SectionName} is not supported in .NET 5. See {IssueLink} for details. For now, it will be disabled.", name, issue);
                    yield return (section, issue);
                }
            }
        }
    }
}
