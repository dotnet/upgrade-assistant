using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;
using AspNetMigrator.ConfigUpdater;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.DefaultConfigUpdaters
{
    public class WebNamespaceMigrator : IConfigUpdater
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

        private readonly ILogger<AppSettingsMigrator> _logger;
        private IEnumerable<string> _namespacesToMigrate;
        private string? _viewImportsPath;

        public string Title => "Migrate system.web.webPages.razor/pages/namespaces";

        public string Description => "Migrate namespaces which are auto-included for web pages to _ViewImports.cshtml";

        // This may add namespaces that don't exist in ASP.NET Core which would increase build breaks,
        // but the risk is still low because it should only add namespaces that are used by the project's
        // views, so work was already needed (either automated or manual) to address those calls and these
        // namespaces should be cleaned up by the same processes.
        public BuildBreakRisk Risk => BuildBreakRisk.Low;

        public WebNamespaceMigrator(ILogger<AppSettingsMigrator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _namespacesToMigrate = Enumerable.Empty<string>();
            _viewImportsPath = null;
        }

        public async Task<bool> ApplyAsync(IMigrationContext context, ImmutableArray<ConfigFile> configFiles, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                var project = await context.GetProjectAsync(token).ConfigureAwait(false);
                if (project is null)
                {
                    _logger.LogError("No project loaded");
                    return false;
                }

                var viewImportsContents = new List<string>(_viewImportsPath is null
                    ? new[] { string.Empty, ViewImportsInitialContent }
                    : await File.ReadAllLinesAsync(_viewImportsPath, token).ConfigureAwait(false));

                foreach (var ns in _namespacesToMigrate.OrderByDescending(s => s))
                {
                    _logger.LogDebug("Namespace {Namespace} added to _ViewImports.cshtml", ns);
                    viewImportsContents.Insert(0, $"{RazorUsingPrefix}{ns}");
                }

                var path = _viewImportsPath ?? Path.Combine(project.Directory ?? string.Empty, ViewImportsRelativePath);
                await File.WriteAllLinesAsync(path, viewImportsContents, token).ConfigureAwait(false);
                _logger.LogInformation("View imports written to {ViewImportsPath}", path);

                return true;
            }
            catch (IOException exc)
            {
                _logger.LogError(exc, "Unexpected exception accessing _ViewImports");
                return false;
            }
        }

        public async Task<bool> IsApplicableAsync(IMigrationContext context, ImmutableArray<ConfigFile> configFiles, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Find namespace imports in config files
            var namespaces = new List<string>();
            foreach (var configFile in configFiles)
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
                                    _logger.LogDebug("Not migrating namespace {NamespaceName}", nsNameValue);
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

            var project = await context.GetProjectAsync(token).ConfigureAwait(false);

            if (project is null)
            {
                throw new ArgumentOutOfRangeException();
            }

            var alreadyImportedNamespaces = new List<string>();
            _viewImportsPath = project.FindFiles(ProjectItemType.Content, ViewImportsRelativePath).FirstOrDefault();

            // Look for a _ViewImports.cstml to see whether any of the namespaces are already imported
            if (_viewImportsPath is null)
            {
                _logger.LogDebug("No _ViewImports.cshtml found in project");
            }
            else
            {
                alreadyImportedNamespaces.AddRange((await File.ReadAllLinesAsync(_viewImportsPath, token).ConfigureAwait(false))
                    .Select(line => line.Trim())
                    .Where(line => line.StartsWith(RazorUsingPrefix, StringComparison.Ordinal))
                    .Select(line => line[RazorUsingPrefix.Length..].Trim()));

                _logger.LogDebug("Found {NamespaceCount} namespaces already in _ViewImports.cshtml", alreadyImportedNamespaces.Count);
            }

            _namespacesToMigrate = namespaces.Distinct().Where(ns => !alreadyImportedNamespaces.Contains(ns));
            _logger.LogInformation("{NamespaceCount} web page namespace imports need migrated: {Namespaces}", _namespacesToMigrate.Count(), string.Join(", ", _namespacesToMigrate));
            return _namespacesToMigrate.Any();
        }
    }
}
