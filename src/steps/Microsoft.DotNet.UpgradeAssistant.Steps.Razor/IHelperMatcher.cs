// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    public interface IHelperMatcher
    {
        Task<IEnumerable<TextReplacement>> GetHelperReplacementsAsync(RazorCodeDocument document);

        Task<bool> HasHelperAsync(RazorCodeDocument document);
    }
}
