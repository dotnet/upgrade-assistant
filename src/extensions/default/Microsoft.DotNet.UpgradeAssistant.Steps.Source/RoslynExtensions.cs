// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class RoslynExtensions
    {
        public static async Task<Document> SimplifyAsync(this Document document, CancellationToken token)
        {
            // Add using declaration if needed
            var updatedImports = await ImportAdder.AddImportsAsync(document, Simplifier.AddImportsAnnotation, cancellationToken: token);

            // Simplify the call, if possible
            var simplified = await Simplifier.ReduceAsync(updatedImports, Simplifier.Annotation, cancellationToken: token);

            // Format document
            var formatted = await Formatter.FormatAsync(simplified, SyntaxAnnotation.ElasticAnnotation, cancellationToken: token);

            return formatted;
        }

        public static async Task<Solution> SimplifyAsync(this Solution sln, CancellationToken token)
        {
            var editor = new SolutionEditor(sln);

            foreach (var project in sln.Projects)
            {
                foreach (var docId in project.DocumentIds)
                {
                    var doc = project.GetDocument(docId);

                    if (doc is not null)
                    {
                        var updatedDoc = await doc.SimplifyAsync(token);

                        if (doc != updatedDoc)
                        {
                            var docEditor = await editor.GetDocumentEditorAsync(docId, token);
                            var updatedRoot = await updatedDoc.GetSyntaxRootAsync(token);

                            if (updatedRoot is not null)
                            {
                                docEditor.ReplaceNode(docEditor.OriginalRoot, updatedRoot);
                            }
                        }
                    }
                }
            }

            return editor.GetChangedSolution();
        }
    }
}
