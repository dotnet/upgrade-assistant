// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
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

            foreach (var file in GetXamlDocuments(roslynProject))
            {
                var sourceText = await file.GetTextAsync(token).ConfigureAwait(false);
                var text = sourceText.ToString();

                // Make replacements...
                foreach (var key in XamarinToMauiReplacementMap.Keys)
                {
                    text = text.Replace(key, XamarinToMauiReplacementMap[key]);
                }

                var newText = SourceText.From(text, encoding: sourceText.Encoding);

                solution = solution.WithAdditionalDocumentText(file.Id, newText);
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
