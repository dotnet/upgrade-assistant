// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.DesignTools.Markup.Metadata;
using Microsoft.VisualStudio.DesignTools.Xaml.LanguageService.RoslynPrototype;
using Microsoft.VisualStudio.DesignTools.Xaml.LanguageService.Semantics;
using static System.Net.WebRequestMethods;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    public class XamlNamespaceUpgradeStep : UpgradeStep
    {
        private static readonly IReadOnlyDictionary<string, string> XamarinToMauiReplacementMap = new Dictionary<string, string>
        {
            { "http://xamarin.com/schemas/2014/forms", "http://schemas.microsoft.com/dotnet/2021/maui" },
            { "http://xamarin.com/schemas/2020/toolkit", "http://schemas.microsoft.com/dotnet/2022/maui/toolkit" },
            { "clr-namespace:Xamarin.Forms.PlatformConfiguration.AndroidSpecific;assembly=Xamarin.Forms.Core", "clr-namespace:Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;assembly=Microsoft.Maui.Controls" },
            { "clr-namespace:Xamarin.Forms.PlatformConfiguration.iOSSpecific;assembly=Xamarin.Forms.Core", "clr-namespace:Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;assembly=Microsoft.Maui.Controls" },
            { "clr-namespace:Xamarin.Forms.PlatformConfiguration.macOSSpecific;assembly=Xamarin.Forms.Core", "clr-namespace:Microsoft.Maui.Controls.PlatformConfiguration.macOSSpecific;assembly=Microsoft.Maui.Controls" },
            { "clr-namespace:Xamarin.Forms.PlatformConfiguration.TizenSpecific;assembly=Xamarin.Forms.Core", "clr-namespace:Microsoft.Maui.Controls.PlatformConfiguration.TizenSpecific;assembly=Microsoft.Maui.Controls" },
            { "clr-namespace:Xamarin.Forms.PlatformConfiguration.WindowsSpecific;assembly=Xamarin.Forms.Core", "clr-namespace:Microsoft.Maui.Controls.PlatformConfiguration.WindowsSpecific;assembly=Microsoft.Maui.Controls" },
        };

        private readonly IPackageRestorer _restorer;
        private readonly XamlTreeProvider _xamlTreeProvider = new();
        private readonly Identifier xmlns = Identifier.For("xmlns");

        public override string Title => "Update XAML Namespaces";

        public override string Description => "Updates XAML namespaces to .NET MAUI";

        public XamlNamespaceUpgradeStep(IPackageRestorer restorer, ILogger<XamlNamespaceUpgradeStep> logger)
            : base(logger)
        {
            _restorer = restorer;
        }

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            WellKnownStepIds.TemplateInserterStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
        };

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();
            var roslynProject = GetBestRoslynProject(project.GetRoslynProject());
            var solution = roslynProject.Solution;
            var compilation = await roslynProject.GetCompilationAsync(token).ConfigureAwait(false);
            compilation = EnsureMauiGraphics(compilation) ?? compilation;

            if (compilation != null)
            {
                foreach (var file in GetXamlDocuments(roslynProject))
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        SourceText? newText = await UpdateXaml(compilation, file, token).ConfigureAwait(false);
                        if (newText != null)
                        {
                            solution = solution.WithAdditionalDocumentText(file.Id, newText);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }
            }

            var status = context.UpdateSolution(solution) ? UpgradeStepStatus.Complete : UpgradeStepStatus.Failed;

            // Remove MauiProgram.cs added by MauiHeadTemplates.json from project file manually again if necessary
            // because WorkAroundRoslynIssue36781 doesn't think it's a duplicate item - but it is.
            var projectFile = project.GetFile();
            if (projectFile.RemoveItem(new ProjectItemDescriptor(ProjectItemType.Compile) { Include = "MauiProgram.cs" }))
            {
                await projectFile.SaveAsync(token).ConfigureAwait(false);
            }

            return new UpgradeStepApplyResult(status, $"Updated XAML namespaces to .NET MAUI");
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

        private async Task<SourceText?> UpdateXaml(Compilation compilation, TextDocument file, CancellationToken token)
        {
            var sourceText = await file.GetTextAsync(token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
            {
                return null;
            }

            XmlTree? xmlTree = GetValidXmlTree(sourceText, file.FilePath, token);
            if (xmlTree == null)
            {
                return null;
            }

            List<(SourceLocation range, string text)> changes = new();
            CollectXmlnsChanges(xmlTree.RootElement, changes);
            if (changes.Count == 0)
            {
                return null; // no XF namespaces thus nothing to update
            }

            // Update xmlns'es and get new XML tree
            var newSourceText = UpdateText(sourceText, changes);
            xmlTree = GetValidXmlTree(newSourceText, file.FilePath, token);
            if (xmlTree == null)
            {
                return null;
            }

            // Build XAML tree
            XamlTree? xamlTree = _xamlTreeProvider.GetXamlTree(compilation, xmlTree, token);
            if (xamlTree == null)
            {
                return newSourceText;
            }

            // Update XAML
            changes = new();
            CollectXamlnsChanges(xamlTree.RootElement, changes);
            newSourceText = UpdateText(newSourceText, changes);
            return newSourceText;
        }

        private static SourceText UpdateText(SourceText sourceText, List<(SourceLocation range, string text)> changes)
        {
            if (changes.Count == 0)
            {
                return sourceText;
            }

            var text = sourceText.ToString();
            StringBuilder builder = new StringBuilder(text.Length);
            changes.Sort((a, b) => a.range.Location.CompareTo(b.range.Location));
            int start = 0;
            foreach (var change in changes)
            {
                int end = change.range.Location;
                string left = text.Substring(start, end - start);
                builder.Append(left);
                builder.Append(change.text);
                start = end + change.range.Length;
            }

            builder.Append(text.Substring(start));
            text = builder.ToString();
            var newSourceText = SourceText.From(text, sourceText.Encoding);
            return newSourceText;
        }

        private XmlTree? GetValidXmlTree(SourceText sourceText, string? path, CancellationToken token)
        {
            if (_xamlTreeProvider.GetXmlTree(sourceText, path, token) is not XmlTree xmlTree ||
                token.IsCancellationRequested || xmlTree.RootElement == null || xmlTree.ParseContext.Errors.Any())
            {
                return null;
            }

            return xmlTree;
        }

        private static void CollectXamlnsChanges(XamlElement element, List<(SourceLocation range, string text)> changes)
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
                        changes.Add((range, "Colors."));
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
                    CollectXamlnsChanges(child, changes);
                }
            }
        }

        private static void CollectMarkupExtensionChanges(XamlMarkupExtensionNode ext, List<(SourceLocation range, string text)> changes)
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
                            changes.Add((range, "Colors."));
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

        private static void CollectXmlnsChanges(XmlElement element, List<(SourceLocation range, string text)> changes)
        {
            if (element.Attributes != null)
            {
                foreach (var attribute in element.Attributes)
                {
                    if (attribute.IsNamespaceAttribute && attribute.Value.Value is string ns &&
                        XamarinToMauiReplacementMap.TryGetValue(ns, out string newns))
                    {
                        // Range for attribute value includes quotes. We need to exclude them.
                        var range = attribute.Value.Range;
                        range = new SourceLocation(range.Location + 1, range.Length - 2);
                        changes.Add((range, newns));
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

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return await Task.Run(() =>
            {
                // With updated TFMs and UseMaui, we need to restore packages
                var project = context.CurrentProject.Required();
                var roslynProject = GetBestRoslynProject(project.GetRoslynProject());
                var hasXamlFiles = GetXamlDocuments(roslynProject).Any();
                if (hasXamlFiles)
                {
                    Logger.LogInformation(".NET MAUI project has XAML files that may need to be updated");
                    return new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, ".NET MAUI project has XAML files that may need to be updated", BuildBreakRisk.High);
                }
                else
                {
                    Logger.LogInformation(".NET MAUI project does not contain any XAML files");
                    return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, ".NET MAUI project does not contain any XAML files", BuildBreakRisk.None);
                }
            });
        }

        protected override async Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                return false;
            }

            if (context.CurrentProject is null)
            {
                return false;
            }

            var project = context.CurrentProject.Required();
            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            if (components.HasFlag(ProjectComponents.MauiAndroid) || components.HasFlag(ProjectComponents.MauiiOS) || components.HasFlag(ProjectComponents.Maui))
            {
                return true;
            }

            return false;
        }

        private static IEnumerable<TextDocument> GetXamlDocuments(Project project)
            => project.AdditionalDocuments.Where(d => d.FilePath?.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) == true);

        private static Project GetBestRoslynProject(Project project)
            => project.Solution.Projects
                .Where(p => p.FilePath == project.FilePath)
                .OrderByDescending(p => p.AdditionalDocumentIds.Count)
                .First();
    }
}
