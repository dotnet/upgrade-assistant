// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class IUpgradeContextExtensions
    {
        public static void AddResultForStep(this IUpgradeContext context, UpgradeStep step,
            string location, UpgradeStepStatus upgradeStepStatus, string message, string? details = null, Uri? helpUri = null, OutputLevel? outputLevel = null)
        {
            AddResult(context, step.Title, step.Description, location, step.Id, upgradeStepStatus, message, details, helpUri, outputLevel);
        }

        public static void AddResult(this IUpgradeContext context, string stepName, string stepDescription, string location, string ruleId,
            UpgradeStepStatus upgradeStepStatus, string message, string? details = null, Uri? helpUri = null, OutputLevel? outputLevel = null)
        {
            var status = upgradeStepStatus switch
            {
                UpgradeStepStatus.Skipped => "Skipped",
                UpgradeStepStatus.Failed => "Failed",
                UpgradeStepStatus.Complete => "Complete",
                UpgradeStepStatus.Incomplete => "Incomplete",
                _ => throw new ArgumentException("Invalid UpgradeStepStatus", nameof(upgradeStepStatus))
            };

            var level = outputLevel ?? upgradeStepStatus switch
            {
                UpgradeStepStatus.Skipped => OutputLevel.Info,
                UpgradeStepStatus.Failed => OutputLevel.Warning,
                UpgradeStepStatus.Complete => OutputLevel.Info,
                UpgradeStepStatus.Incomplete => OutputLevel.Info,
                _ => throw new ArgumentException("Invalid UpgradeStepStatus", nameof(upgradeStepStatus))
            };

            var result = new OutputResult()
            {
                Level = level,
                FileLocation = location,
                RuleId = ruleId,
                ResultMessage = string.IsNullOrEmpty(details) ? $"{status}: {message}" : $"{status}: {message}{Environment.NewLine}{details}",
                FullDescription = stepDescription,
                HelpUri = helpUri
            };

            var outputResultDefinition = new OutputResultDefinition()
            {
                Name = stepName,
                InformationUri = WellKnownDocumentationUrls.UpgradeAssistantUsage,
                Results = ImmutableList.Create(result).ToAsyncEnumerable()
            };

            context.Results.Add(outputResultDefinition);
        }
    }
}
