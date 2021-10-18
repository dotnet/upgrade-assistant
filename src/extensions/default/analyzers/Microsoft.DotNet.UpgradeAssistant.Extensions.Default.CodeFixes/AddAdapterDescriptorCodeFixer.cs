// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class AddAdapterDescriptorCodeFixer : CodeFixProvider
    {
        private const string AdapterDescriptorResourceName = "Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes.Templates.AdapterDescriptor.cs";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AdapterDescriptorTypeAnalyzer.AddAdapterDescriptorDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            // Multiple documents don't seem to merge well
            return null!;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root is null)
            {
                return;
            }

            var node = root.FindNode(context.Span);

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AddAdapterDescriptorTitle,
                    async cancellationToken =>
                    {
                        var project = context.Document.Project;

                        using var sr = new StreamReader(typeof(AddAdapterDescriptorCodeFixer).Assembly.GetManifestResourceStream(AdapterDescriptorResourceName));
                        var contents = await sr.ReadToEndAsync().ConfigureAwait(false);
                        contents = contents.Replace("/*{{DEPRECATED_TYPE}}*/", node.ToFullString().Trim());

                        var adapterDescriptorAttributeClass = project.AddDocument("AdapterDescriptor.cs", contents);
                        project = adapterDescriptorAttributeClass.Project;
                        var slnEditor = new SolutionEditor(project.Solution);

                        return slnEditor.GetChangedSolution()
                            .WithDocumentText(adapterDescriptorAttributeClass.Id, await adapterDescriptorAttributeClass.GetTextAsync(cancellationToken).ConfigureAwait(false));
                    },
                    nameof(AddAdapterDescriptorCodeFixer)),
                context.Diagnostics);
        }
    }
}
