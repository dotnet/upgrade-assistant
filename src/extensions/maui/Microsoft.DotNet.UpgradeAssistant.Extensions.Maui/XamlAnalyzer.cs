// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.DesignTools.Markup.Metadata;
using Microsoft.VisualStudio.DesignTools.Xaml.LanguageService.Semantics;
using Microsoft.VisualStudio.DesignTools.XamlAnalyzer;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{

    [ApplicableComponents(ProjectComponents.Maui)]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class XamlAnalyzer : XamlAnalyzerBase
    {
        public const string NewText = nameof(NewText);
        public const string FilePath = nameof(FilePath);

        public const string DiagnosticId = "UA0020";
        private const string Category = "Upgrade";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.XamlAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.XamlsAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.XamlAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void AnalyzeAdditoinalFile(AdditionalFileAnalysisContext context)
        {
            try
            {
                Analyze(context);
            }
            catch
            {
            }
        }

        private void Analyze(AdditionalFileAnalysisContext context)
        {
            AdditionalText file = context.AdditionalFile;
            if (!file.Path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (file.GetText() is not SourceText sourceText)
            {
                return;
            }

            XmlTree? xmlTree = GetValidXmlTree(sourceText, file.Path, context.CancellationToken);
            if (xmlTree == null)
            {
                return;
            }

            // Collect namespace replacements
            List<TextChange> xmlChanges = new();
            CollectXmlnsChanges(xmlTree.RootElement, xmlChanges);
            if (xmlChanges.Count == 0)
            {
                return;
            }

            // Create XAML containing new namespaces
            var newText = GetUpdatedText(sourceText, xmlChanges);
            var newSourceText = SourceText.From(newText, sourceText.Encoding);
            xmlTree = GetValidXmlTree(newSourceText, file.Path, context.CancellationToken);
            if (xmlTree == null)
            {
                return;
            }

            var compilation = EnsureMauiGraphics(context.Compilation);
            XamlTree xamlTree = this.XamlTreeProvider.GetXamlTree(compilation, xmlTree, context.CancellationToken);
            List<TextChange> xamlChanges = new();
            CollectXamlChanges(xamlTree.RootElement, xamlChanges);
            newText = GetUpdatedText(newSourceText, xamlChanges);

            // Create "replace all text" action
            TextSpan span = new TextSpan(0, sourceText.Length);
            LinePositionSpan lineSpan = sourceText.Lines.GetLinePositionSpan(span);
            var location = Location.Create(context.AdditionalFile.Path, span, lineSpan);
            var props = ImmutableDictionary.CreateRange(new KeyValuePair<string, string?>[] { new(XamlAnalyzer.NewText, newText) });
            var diagnostic = Diagnostic.Create(XamlAnalyzer.Rule, location, props);
            context.ReportDiagnostic(diagnostic);
        }

        private static string GetUpdatedText(SourceText sourceText, List<TextChange> changes)
        {
            var text = sourceText.ToString();
            if (changes.Count == 0)
            {
                return text;
            }

            StringBuilder builder = new StringBuilder(text.Length);
            changes.Sort((a, b) => a.Offset.CompareTo(b.Offset));
            int start = 0;
            foreach (var change in changes)
            {
                int end = change.Offset;
                string left = text.Substring(start, end - start);
                builder.Append(left);
                builder.Append(change.Text);
                start = end + change.Length;
            }

            builder.Append(text.Substring(start));
            var newSourceText = builder.ToString();
            return newSourceText;
        }

        private static void CollectXmlnsChanges(XmlElement element, List<TextChange> changes)
        {
            if (element.Attributes != null)
            {
                foreach (var attribute in element.Attributes)
                {
                    if (attribute.IsNamespaceAttribute && attribute.Value.Value is string ns &&
                        XamlNamespaceUpgradeStep.XamarinToMauiReplacementMap.TryGetValue(ns, out string newns))
                    {
                        // Range for attribute value includes quotes. We need to exclude them.
                        var range = attribute.Value.Range;
                        var change = new TextChange(range.Location + 1, range.Length - 2, newns);
                        changes.Add(change);
                    }
                }
            }

            if (element.Children is var children)
            {
                foreach (var child in children)
                {
                    CollectXmlnsChanges(child, changes);
                }
            }
        }

        private static void CollectXamlChanges(XamlElement element, List<TextChange> changes)
        {
            // Example: <x:StaticExtension> Color.Red </x:StaticExtension>
            if (element.AsObject is XamlObjectElement xamlObject &&
                xamlObject.Type?.RuntimeType?.Equals(XamlTypes.StaticExtension) == true)
            {
                if (element.Children?.FirstOrDefault() is XamlObjectElement child &&
                    child.Text is string value)
                {
                    const string color = "Color.";

                    // Account for leading white spaces, e.g. "  Color.Red"
                    int index = value.IndexOf(color, StringComparison.Ordinal);
                    bool isOkay = true;
                    for (int i = index - 1; i >= 0 && isOkay; i--)
                    {
                        isOkay = char.IsWhiteSpace(value[index]);
                    }

                    if (isOkay)
                    {
                        SourceLocation range = new(child.Range.Location + index, color.Length);
                        changes.Add(new TextChange(range, "Colors."));
                    }
                }

                return;
            }

            if (element.Attributes != null)
            {
                foreach (var attribute in element.Attributes)
                {
                    var ext = attribute.MarkupExtension;
                    if (ext != null)
                    {
                        CollectMarkupExtensionChanges(ext, changes);
                    }
                }
            }

            if (element.Children != null)
            {
                foreach (var child in element.Children)
                {
                    CollectXamlChanges(child, changes);
                }
            }
        }

        private static void CollectMarkupExtensionChanges(XamlMarkupExtensionNode ext, List<TextChange> changes)
        {
            if (ext.Values is not XamlMarkupExtensionValue[] nodes || nodes.Length == 0)
            {
                return;
            }

            // Replace Color with Colors, e.g. "{x:Static Color.Red}" => "{x:Static Colors.Red}"
            // Note that we do not handle "{x:Static xf:Color.Red}" because we do not fix
            // xmlns:xf="clr-namespace:Xamarin.Forms;assembly=Xamarin.FormsCore" (because
            // it maps into multiple MAUI namespaces).
            if (ext.Type?.RuntimeType?.Equals(XamlTypes.StaticExtension) == true)
            {
                if (nodes.Length == 1)
                {
                    var node = nodes[0];
                    if (!node.IsMarkupExtension && node.Value is string value)
                    {
                        const string color = "Color.";
                        if (value.StartsWith(color, StringComparison.Ordinal))
                        {
                            SourceLocation range = new(node.ValueRange.Location, color.Length);
                            changes.Add(new TextChange(range, "Colors."));
                        }
                    }
                }

                return;
            }

            // Look for nested extensions, e.g. {Binding Source={x:Static Color.Pink}}
            foreach (var node in nodes)
            {
                var childExt = node.MarkupExtension;
                if (childExt != null)
                {
                    CollectMarkupExtensionChanges(childExt, changes);
                }
            }
        }

        private XmlTree? GetValidXmlTree(SourceText sourceText, string? path, CancellationToken token)
        {
            if (this.XamlTreeProvider.GetXmlTree(sourceText, path, token) is not XmlTree xmlTree ||
                token.IsCancellationRequested || xmlTree.RootElement == null || xmlTree.ParseContext.Errors.Any())
            {
                return null;
            }

            return xmlTree;
        }

        private static Compilation? EnsureMauiGraphics(Compilation? compilation)
        {
            if (compilation == null)
            {
                return null;
            }

            PortableExecutableReference? mauiControls = null;
            foreach (PortableExecutableReference pr in compilation.References)
            {
                if (pr.FilePath is string path)
                {
                    if (path.EndsWith(@"\Microsoft.Maui.Graphics.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        return compilation;
                    }

                    if (mauiControls == null && path.EndsWith(@"\Microsoft.Maui.Controls.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        mauiControls = pr;
                    }
                }
            }

            if (mauiControls == null)
            {
                return compilation;
            }

            // Example:
            //  BEFORE: C:\Program Files\dotnet\packs\Microsoft.Maui.Controls.Ref.android\7.0.55\ref\net7.0-android30.0\Microsoft.Maui.Controls.dll
            //  AFTER:  C:\Program Files\dotnet\packs\Microsoft.Maui.Sdk\7.0.55\Sdk\Microsoft.Maui.Graphics.dll
            string[] parts = mauiControls.FilePath?.Split('\\') ?? Array.Empty<string>();
            for (int i = 2; i < parts.Length; i++)
            {
                if (Version.TryParse(parts[i], out Version ver) && ver.Major >= 5 && ver.Major <= 8) // e.g. 7.0.55
                {
                    string path = string.Join("\\", parts.Take(i - 1));
                    path = string.Join("\\", path, "Microsoft.Maui.Sdk", parts[i], "Sdk\\Microsoft.Maui.Graphics.dll");
                    if (System.IO.File.Exists(path))
                    {
                        var graphics = PortableExecutableReference.CreateFromFile(path);
                        compilation = compilation.AddReferences(graphics);
                    }

                    break;
                }
            }

            return compilation;
        }

        [DebuggerDisplay("{Offset}, {Text,nq}")]
        private struct TextChange
        {
            public TextChange(int offset, int length, string text)
            {
                this.Offset = offset;
                this.Length = length;
                this.Text = text;
            }

            public TextChange(SourceLocation loc, string text)
            {
                this.Offset = loc.Location;
                this.Length = loc.Length;
                this.Text = text;
            }

            public readonly int Offset;
            public readonly int Length;
            public readonly string Text;

            public TextChange Shift(int offset) => new(Offset + offset, Length, Text);
        }
    }
}
