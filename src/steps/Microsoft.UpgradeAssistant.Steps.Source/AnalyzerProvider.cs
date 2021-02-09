using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AspNetMigrator.Extensions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.SourceUpdater
{
    public class AnalyzerProvider
    {
        private const string AssemblySearchPattern = "*.dll";
        private const string SourceUpdaterOptionsSectionName = "SourceUpdater";

        private readonly AggregateExtensionProvider _extensions;
        private readonly ILogger<AnalyzerProvider> _logger;
        private readonly IServiceProvider _serviceProvider;

        public AnalyzerProvider(AggregateExtensionProvider extensions, IServiceProvider serviceProvider, ILogger<AnalyzerProvider> logger)
        {
            _extensions = extensions ?? throw new ArgumentNullException(nameof(extensions));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<DiagnosticAnalyzer> GetAnalyzers()
        {
            var analyzers = new List<DiagnosticAnalyzer>();

            foreach (var extension in _extensions.ExtensionProviders)
            {
                _logger.LogDebug("Looking for analyzers in {Extension}", extension.Name);

                var sourceUpdaterOptions = extension.GetOptions<SourceUpdaterOptions>(SourceUpdaterOptionsSectionName);
                if (sourceUpdaterOptions?.SourceUpdaterPath is null)
                {
                    _logger.LogDebug("No source updater section in extension manifest. Finished loading analyzers from {Extension}", extension.Name);
                    continue;
                }

                foreach (var file in extension.GetFiles(sourceUpdaterOptions.SourceUpdaterPath, AssemblySearchPattern))
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(file);

                        var newAnalzyers = assembly.GetTypes()
                            .Where(t => t.IsPublic && t.CustomAttributes.Any(a => a.AttributeType.Equals(typeof(DiagnosticAnalyzerAttribute))))
                            .Select(t => ActivatorUtilities.CreateInstance(_serviceProvider, t))
                            .Cast<DiagnosticAnalyzer>();

                        _logger.LogDebug("Loaded {Count} analyzers from {AssemblyName}", newAnalzyers.Count(), assembly.FullName);
                        analyzers.AddRange(newAnalzyers);
                    }
                    catch (FileLoadException)
                    {
                    }
                    catch (BadImageFormatException)
                    {
                    }
                }

                _logger.LogDebug("Finished loading analyzers from {Extension}", extension.Name);
            }

            _logger.LogDebug("Loaded {Count} analyzers", analyzers.Count);

            return analyzers;
        }

        public IEnumerable<CodeFixProvider> GetCodeFixProviders()
        {
            var codeFixProviders = new List<CodeFixProvider>();

            foreach (var extension in _extensions.ExtensionProviders)
            {
                _logger.LogDebug("Looking for code fix providers in {Extension}", extension.Name);

                var sourceUpdaterOptions = extension.GetOptions<SourceUpdaterOptions>(SourceUpdaterOptionsSectionName);
                if (sourceUpdaterOptions?.SourceUpdaterPath is null)
                {
                    _logger.LogDebug("No source updater section in extension manifest. Finished loading code fix providers from {Extension}", extension.Name);
                    continue;
                }

                foreach (var file in extension.GetFiles(sourceUpdaterOptions.SourceUpdaterPath, AssemblySearchPattern))
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(file);

                        var newCodeFixProviders = assembly.GetTypes()
                            .Where(t => t.IsPublic && t.CustomAttributes.Any(a => a.AttributeType.Equals(typeof(ExportCodeFixProviderAttribute))))
                            .Select(t => ActivatorUtilities.CreateInstance(_serviceProvider, t))
                            .Cast<CodeFixProvider>();

                        _logger.LogDebug("Loaded {Count} code fix providers from {AssemblyName}", newCodeFixProviders.Count(), assembly.FullName);
                        codeFixProviders.AddRange(newCodeFixProviders);
                    }
                    catch (FileLoadException)
                    {
                    }
                    catch (BadImageFormatException)
                    {
                    }
                }

                _logger.LogDebug("Finished loading code fix providers from {Extension}", extension.Name);
            }

            _logger.LogDebug("Loaded {Count} code fix providers", codeFixProviders.Count);

            return codeFixProviders;
        }
    }
}
