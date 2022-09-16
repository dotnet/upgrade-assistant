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
            string location, UpgradeStepStatus upgradeStepStatus, string message)
        {
            AddResult(context, step.Title, location, step.Id, upgradeStepStatus, message);
        }

        public static void AddResult(this IUpgradeContext context, string stepName, string location, string ruleId,
            UpgradeStepStatus upgradeStepStatus, string message)
        {
            var status = upgradeStepStatus switch
            {
                UpgradeStepStatus.Skipped => "Skipped",
                UpgradeStepStatus.Failed => "Failed",
                UpgradeStepStatus.Complete => "Complete",
                UpgradeStepStatus.Incomplete => "Incomplete",
                _ => throw new ArgumentException("Invalid UpgradeStepStatus", nameof(upgradeStepStatus))
            };

            var result = new OutputResult()
            {
                FileLocation = location,
                RuleId = ruleId,
                ResultMessage = $"{status}: {message}",
                FullDescription = message,
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
