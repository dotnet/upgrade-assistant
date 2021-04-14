// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.ConfigUpdaters
{
    [ApplicableComponents(ProjectComponents.AspNetCore)]
    public class WebNamespaceConfigUpdater : IUpdater<ConfigFile>
    {
        private const string NamespacesPath = "/configuration/system.web.webPages.razor/pages/namespaces";
        private const string AddElementName = "add";
        private const string NamespaceAttributeName = "namespace";
        private const string ViewImportsRelativePath = @"Views\_ViewImports.cshtml";
        private const string ViewImportsInitialContent = "@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers";
        private const string RazorUsingPrefix = "@using ";

        private static readonly string[] NamespacesToDrop = new[]
        {
            "System.Web"
        };

        private readonly ILogger<AppSettingsConfigUpdater> _logger;
        private IEnumerable<string> _namespacesToUpgrade;
        private string? _viewImportsPath;

        public string Id => typeof(WebNamespaceConfigUpdater).FullName!;

        public string Title => "Convert system.web.webPages.razor/pages/namespaces";

        public string Description => "Convert namespaces which are auto-included for web pages to _ViewImports.cshtml";

        // This may add namespaces that don't exist in ASP.NET Core which would increase build breaks,
        // but the risk is still low because it should only add namespaces that are used by the project's
        // views, so work was already needed (either automated or manual) to address those calls and these
        // namespaces should be cleaned up by the same processes.
        public BuildBreakRisk Risk => BuildBreakRisk.Low;

        public WebNamespaceConfigUpdater(ILogger<AppSettingsConfigUpdater> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _namespacesToUpgrade = Enumerable.Empty<string>();
            _viewImportsPath = null;
        }

        public Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<ConfigFile> inputs, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                var project = context.CurrentProject.Required();

                var viewImportsContents = new List<string>(_viewImportsPath is null
                    ? new[] { string.Empty, ViewImportsInitialContent }
                    : File.ReadAllLines(_viewImportsPath));

                foreach (var ns in _namespacesToUpgrade.OrderByDescending(s => s))
                {
                    _logger.LogDebug("Namespace {Namespace} added to _ViewImports.cshtml", ns);
                    viewImportsContents.Insert(0, $"{RazorUsingPrefix}{ns}");
                }

                var path = _viewImportsPath ?? Path.Combine(project.FileInfo.DirectoryName ?? string.Empty, ViewImportsRelativePath);
                File.WriteAllLines(path, viewImportsContents);
                _logger.LogInformation("View imports written to {ViewImportsPath}", path);

                return Task.FromResult<IUpdaterResult>(new DefaultUpdaterResult(true));
            }
            catch (IOException exc)
            {
                _logger.LogError(exc, "Unexpected exception accessing _ViewImports");
                return Task.FromResult<IUpdaterResult>(new DefaultUpdaterResult(false));
            }
        }

        public Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<ConfigFile> inputs, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Find namespace imports in config files
            var namespaces = new List<string>();
            foreach (var configFile in inputs)
            {
                var namespacesElement = configFile.Contents.XPathSelectElement(NamespacesPath);
                if (namespacesElement is not null)
                {
                    foreach (var ns in namespacesElement.Elements(AddElementName))
                    {
                        if (ns is not null)
                        {
                            var nsName = ns.Attribute(NamespaceAttributeName);
                            if (nsName is not null)
                            {
                                var nsNameValue = nsName.Value;
                                _logger.LogDebug("Found namespace {NamespaceName} in {ConfigFilePath}", nsNameValue, configFile.Path);
                                if (NamespacesToDrop.Any(s => nsNameValue.Equals(s, StringComparison.Ordinal) || nsNameValue.StartsWith($"{s}.", StringComparison.Ordinal)))
                                {
                                    _logger.LogDebug("Not upgrading namespace {NamespaceName}", nsNameValue);
                                }
                                else
                                {
                                    namespaces.Add(nsNameValue);
                                }
                            }
                        }
                    }
                }
            }

            _logger.LogDebug("Found {NamespaceCount} namespaces imported into web pages in config files", namespaces.Count);

            var project = context.CurrentProject.Required();

            var alreadyImportedNamespaces = new List<string>();
            _viewImportsPath = project.FindFiles(ProjectItemType.Content, ViewImportsRelativePath).FirstOrDefault();

            // Look for a _ViewImports.cstml to see whether any of the namespaces are already imported
            if (_viewImportsPath is null)
            {
                _logger.LogDebug("No _ViewImports.cshtml found in project");
            }
            else
            {
                alreadyImportedNamespaces.AddRange(File.ReadAllLines(_viewImportsPath)
                    .Select(line => line.Trim())
                    .Where(line => line.StartsWith(RazorUsingPrefix, StringComparison.Ordinal))
                    .Select(line => line.Substring(RazorUsingPrefix.Length).Trim()));

                _logger.LogDebug("Found {NamespaceCount} namespaces already in _ViewImports.cshtml", alreadyImportedNamespaces.Count);
            }

            _namespacesToUpgrade = namespaces.Distinct().Where(ns => !alreadyImportedNamespaces.Contains(ns));
            _logger.LogInformation("{NamespaceCount} web page namespace imports need upgraded: {Namespaces}", _namespacesToUpgrade.Count(), string.Join(", ", _namespacesToUpgrade));
            return Task.FromResult<IUpdaterResult>(new DefaultUpdaterResult(_namespacesToUpgrade.Any()));
        }
    }
}
