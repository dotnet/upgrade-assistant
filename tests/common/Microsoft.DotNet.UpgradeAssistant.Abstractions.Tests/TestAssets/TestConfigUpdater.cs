// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Abstractions.Tests.TestAssets
{
    [ApplicableComponents(ProjectComponents.AspNetCore)]
    [ApplicableLanguage(Language.CSharp, Language.FSharp)]
    public class TestConfigUpdater : IConfigUpdater
    {
        public string Id => throw new System.NotImplementedException();

        public string Title => throw new System.NotImplementedException();

        public string Description => throw new System.NotImplementedException();

        public BuildBreakRisk Risk => throw new System.NotImplementedException();

        public Task<bool> ApplyAsync(IUpgradeContext context, ImmutableArray<ConfigFile> configFiles, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> IsApplicableAsync(IUpgradeContext context, ImmutableArray<ConfigFile> configFiles, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}
