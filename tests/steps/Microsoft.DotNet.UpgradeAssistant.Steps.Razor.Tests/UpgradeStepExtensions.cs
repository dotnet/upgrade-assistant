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
