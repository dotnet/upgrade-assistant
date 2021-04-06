// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Fixtures;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild.Tests
{
    /// <summary>
    /// Class (which is never instantiated) for collecting package step tests that
    /// need test fixtures to register MSBuild and make NuGet packages available.
    /// </summary>
    [CollectionDefinition(Name)]
    public class MSBuildStepTestCollection : ICollectionFixture<MSBuildRegistrationFixture>
    {
        // by design this class must be implemented in every test project that uses the MSBuildRegistrationFixture
        // https://github.com/xunit/xunit/issues/409
        public const string Name = "Package Step Tests";
    }
}
