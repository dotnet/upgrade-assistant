// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    public class MauiXAMLCodeFixStep : UpgradeStep
    {
        public override string Title => ".NET MAUI XAML Fixer";

        public override string Description => "Updates XAML namespaces for .NET MAUI projects";

        public MauiXAMLCodeFixStep(ILogger<MauiXAMLCodeFixStep> logger)
            : base(logger)
        {
        }

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            WellKnownStepIds.BackupStepId,
            WellKnownStepIds.TryConvertProjectConverterStepId,
            WellKnownStepIds.SetTFMStepId,
            WellKnownStepIds.PackageUpdaterStepId
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
            WellKnownStepIds.SourceUpdaterStepId
        };

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var projectDir = GetProjectDir(context);
            var xamlFilesFound = Directory.GetFiles(projectDir, "*.xaml");

            foreach (var xamlFile in xamlFilesFound)
            {
                var content = File.ReadAllText(xamlFile);
                content = content.Replace("\"http://xamarin.com/schemas/2014/forms\"", "\"http://schemas.microsoft.com/dotnet/2021/maui\"");
                content = content.Replace("\"http://xamarin.com/schemas/2020/toolkit\"", "\"http://schemas.microsoft.com/dotnet/2022/maui/toolkit\"");
                var doc = new XmlDocument();
                doc.LoadXml(content);
                doc.Save(xamlFile);

                /***
                 * use this logic for other XAML API updates
                ***/

                // var elements = doc.SelectNodes("<xpath to property>");
                // var value = element.GetAttribute(...);
                // element.RemoveAttribute(...);
                // element.SetAttribute(...);
                // doc.Save(file);
            }

            Logger.LogInformation("Updated .NET MAUI XAML namespace successfully");
            return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"to .NET MAUI project "));
        }

        private static string GetProjectDir(IUpgradeContext context)
        {
            return context.CurrentProject.Required().FileInfo.DirectoryName;
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var projectDir = GetProjectDir(context);
            var xamlFileCheck = Directory.GetFiles(projectDir, "*.xaml").FirstOrDefault();
            bool xamlUpdated = true;
            var content = File.ReadAllText(xamlFileCheck);
            if (content.Contains("xmlns=\"http://xamarin.com/schemas/2014/forms\""))
            {
                xamlUpdated = false;
            }

            if (xamlUpdated)
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, ".NET MAUI XAML files namespace is updated", BuildBreakRisk.None));
            }
            else
            {
                Logger.LogInformation(".NET MAUI XAML namespaces need to be updated.");
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, ".NET MAUI XAML namespaces need to be updated.", BuildBreakRisk.High));
            }
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
    }
}
