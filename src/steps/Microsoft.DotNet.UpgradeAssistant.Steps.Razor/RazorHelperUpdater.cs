// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if false

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    public class RazorHelperUpdater : IUpdater<RazorCodeDocument>
    {
        public string Id => typeof(RazorHelperUpdater).FullName!;

        public string Title => "Replace @helper methods with local methods";

        public string Description => "Replace @helper methods with equivalent local methods";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<RazorCodeDocument> inputs, CancellationToken token)
        {
            return new FileUpdaterResult(true, Enumerable.Empty<string>());
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<RazorCodeDocument> inputs, CancellationToken token)
        {
            return new FileUpdaterResult(true, Enumerable.Empty<string>());
        }
    }
}

#endif
