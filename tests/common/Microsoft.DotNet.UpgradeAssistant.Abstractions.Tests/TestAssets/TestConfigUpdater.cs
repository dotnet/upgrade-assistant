// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Abstractions.Tests.TestAssets
{
    [ApplicableComponents(ProjectComponents.AspNetCore)]
    [ApplicableLanguage(Language.CSharp, Language.FSharp)]
    public class TestConfigUpdater : IUpdater<ConfigFile>
    {
        public string Id => throw new System.InvalidOperationException("I am a mock");

        public string Title => throw new System.InvalidOperationException("I am a mock");

        public string Description => throw new System.InvalidOperationException("I am a mock");

        public BuildBreakRisk Risk => throw new System.InvalidOperationException("I am a mock");

        public Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<ConfigFile> inputs, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<ConfigFile> inputs, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}
