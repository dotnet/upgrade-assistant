// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.DesignTools.Markup.Metadata;
using Microsoft.VisualStudio.DesignTools.Xaml.LanguageService.RoslynPrototype;
using Microsoft.VisualStudio.DesignTools.Xaml.LanguageService.Semantics;
using Microsoft.VisualStudio.DesignTools.XamlAnalyzer;
using static System.Net.Mime.MediaTypeNames;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    [DebuggerDisplay("{Offset}, {Text,nq}")]
    public struct Change
    {
        public Change(int offset, int length, string text)
        {
            this.Offset = offset;
            this.Length = length;
            this.Text = text;
        }

        public Change(SourceLocation loc, string text)
        {
            this.Offset = loc.Location;
            this.Length = loc.Length;
            this.Text = text;
        }

        public readonly int Offset;
        public readonly int Length;
        public readonly string Text;

        public Change Shift(int offset) => new(Offset + offset, Length, Text);

        public string Serialize() => $"{Offset},{Length},{Text}";

        public static bool TryParse(string value, out Change change)
        {
            change = default;

            int first = value.IndexOf(',');
            int second = value.IndexOf(',', first + 1);
            if (first <= 0 || second <= first ||
                !int.TryParse(value.Substring(0, first), out var offset) ||
                !int.TryParse(value.Substring(first + 1, second - first - 1), out var length))
            {
                return false;
            }

            string text = value.Substring(second + 1);
            change = new Change(offset, length, text);
            return true;
        }

        public static string Serialize(IEnumerable<Change> changes) => string.Join("|", changes.Select(c => c.Serialize()));
        
        public static bool TryParse(string value, out IReadOnlyList<Change> changes)
        {
            changes = Array.Empty<Change>();

            string[] parts = value.Split('|');
            List<Change> list = new List<Change>(parts.Length);
            foreach (var part in parts)
            {
                if (Change.TryParse(part, out Change change))
                {
                    list.Add(change);
                }
                else
                {
                    return false;
                }
            }

            changes = list;
            return true;
        }

    }

    [ApplicableComponents(ProjectComponents.Maui)]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class XamlAnalyzer : XamlAnalyzerBase
    {
        public const string DiagnosticId = "UA0020";
        private const string Category = "Upgrade";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.XamlAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.XamlsAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.XamlAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public XamlAnalyzer()
        {
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            base.Initialize(context);
        }

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
            List<Change> xmlChanges = new();
            CollectXmlnsChanges(xmlTree.RootElement, xmlChanges);
            if (xmlChanges.Count == 0)
            {
                return;
            }

            // Create XAML containing new namespaces
            var newSourceText = UpdateText(sourceText, xmlChanges);
            xmlTree = GetValidXmlTree(newSourceText, file.Path, context.CancellationToken);
            if (xmlTree == null)
            {
                return;
            }

            var compilation = EnsureMauiGraphics(context.Compilation);
            XamlTree xamlTree = this.XamlTreeProvider.GetXamlTree(compilation, xmlTree, context.CancellationToken);
            List<Change> xamlChanges = new();
            CollectXamlChanges(xamlTree.RootElement, xamlChanges);

            // Remap XAML changes back to original text
            List<Change> changes = new List<Change>(xamlChanges);
            UpdateRanges(xmlChanges, changes);
            changes.AddRange(xmlChanges);
            changes.Sort((a, b) => a.Offset.CompareTo(b.Offset));
            string allChanges = Change.Serialize(changes);

            // Create single action
            TextSpan span = new TextSpan(0, sourceText.Length);
            LinePositionSpan lineSpan = sourceText.Lines.GetLinePositionSpan(span);
            var location = Location.Create(context.AdditionalFile.Path, span, lineSpan);
            var props = ImmutableDictionary.CreateRange(new KeyValuePair<string, string?>[]
            {
                new("Changes", allChanges),
                new("Path", context.AdditionalFile.Path)
            });
            var diagnostic = Diagnostic.Create(XamlAnalyzer.Rule, location, props);
            context.ReportDiagnostic(diagnostic);
        }

        private static void UpdateRanges(List<Change> xmlChanges, List<Change> changes)
        {
            xmlChanges.Sort((a, b) => a.Offset.CompareTo(b.Offset));
            changes.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            List<(int offset, int shift)> shifts = new(xmlChanges.Count);
            int runningDelta = 0;
            for (int i = 0; i < xmlChanges.Count; i++)
            {
                var change = xmlChanges[i];
                var offset = change.Offset + runningDelta;
                runningDelta += change.Text.Length - change.Length;
                shifts.Add((offset, runningDelta));
            }

            for (int i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                (int offset, int shift) item = (change.Offset, 0);
                int index = shifts.BinarySearch(item);
                if (index < 0)
                {
                    index = ~index - 1;
                }

                item = shifts[index];
                changes[i] = change.Shift(-item.shift);
            }
        }

        private static SourceText UpdateText(SourceText sourceText, List<Change> changes)
        {
            if (changes.Count == 0)
            {
                return sourceText;
            }

            var text = sourceText.ToString();
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
            text = builder.ToString();
            var newSourceText = SourceText.From(text, sourceText.Encoding);
            return newSourceText;
        }

        private static void CollectXmlnsChanges(XmlElement element, List<Change> changes)
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
                        var change = new Change(range.Location + 1, range.Length - 2, newns);
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

        private static void CollectXamlChanges(XamlElement element, List<Change> changes)
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
                        changes.Add(new Change(range, "Colors."));
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

        private static void CollectMarkupExtensionChanges(XamlMarkupExtensionNode ext, List<Change> changes)
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
                            changes.Add(new Change(range, "Colors."));
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
    }
}
