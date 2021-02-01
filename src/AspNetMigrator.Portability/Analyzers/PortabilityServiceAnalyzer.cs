using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using AspNetMigrator.Portability.Service;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.Portability.Analyzers
{
    internal class PortabilityServiceAnalyzer : IPortabilityAnalyzer
    {
        private readonly IPortabilityService _service;
        private readonly Dictionary<string, string>? _portabilityComponents;
        private readonly ILogger<PortabilityServiceAnalyzer> _logger;

        public PortabilityServiceAnalyzer(IPortabilityService service, ILogger<PortabilityServiceAnalyzer> logger)
        {
            _service = service;
            _logger = logger;
            _portabilityComponents = PortabilityComponents.GetPortabilityComponentsConfig(logger);
        }

        public async IAsyncEnumerable<PortabilityResult> Analyze(Compilation compilation, [EnumeratorCancellation] CancellationToken token)
        {
            var contains = await GetMemberAccessSymbols(compilation, token)
                .Select(s => s?.GetDocumentationCommentId())
                .Where(s => s is not null)
                .Distinct()
                .ToListAsync(token).ConfigureAwait(false);

            await foreach (var api in _service.GetApiInformation(contains!, token))
            {
                if (!api.IsSupported())
                {
                    api.Component = GetApiComponent(api.Definition.FullName);
                    yield return new PortabilityResult(ApiType.Method, api.Definition.FullName, api.RecommendedChanges, api.Component);
                }
            }
        }

        private static async IAsyncEnumerable<ISymbol> GetMemberAccessSymbols(Compilation compilation, [EnumeratorCancellation] CancellationToken token)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var semantic = compilation.GetSemanticModel(tree);
                var root = await tree.GetRootAsync(token).ConfigureAwait(false);
                var symbols = root.DescendantNodes(_ => true)
                    .Where(node => node.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression))
                    .Select(s => semantic.GetSymbolInfo(s).Symbol);

                foreach (var s in symbols)
                {
                    yield return s;
                }
            }
        }

        private string GetApiComponent(string apiname)
        {
            string apiNamespace = string.Join(".", apiname.Split(".", 3).SkipLast(1));
            if (_portabilityComponents!.TryGetValue(apiNamespace, out string? component))
            {
                return component;
            }
            else
            {
                _logger.LogWarning("Missing Component details for {apiname} ", apiname);
                return string.Empty;
            }
        }
    }
}
