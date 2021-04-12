// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.DotNet.UpgradeAssistant.Steps.Razor;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class UpgradeStepExtensions
    {
        public static void SetStatus(this UpgradeStep step, UpgradeStepStatus status)
        {
            var method = typeof(UpgradeStep).GetProperty(nameof(UpgradeStep.Status))?.GetSetMethod(nonPublic: true);

            if (method is null)
            {
                throw new InvalidOperationException("Could not find UpgradeStep.Status property");
            }

            method.Invoke(step, new object[] { status });
        }

        public static void ClearRazorDocuments(this RazorUpdaterStep step)
        {
            var field = typeof(RazorUpdaterStep).GetField("_razorDocuments", BindingFlags.NonPublic | BindingFlags.Instance);

            if (field is null)
            {
                throw new InvalidOperationException("Could not find RazorUpdaterStep._razorDocuments field");
            }

            var documents = field.GetValue(step) as Dictionary<string, RazorCodeDocument>;
            documents?.Clear();
        }
    }
}
