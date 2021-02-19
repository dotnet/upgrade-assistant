// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Fixtures;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Tests
{
    /// <summary>
    /// Class (which is never instantiated) for collecting package step tests that
    /// need test fixtures to register MSBuild and make NuGet packages available.
    /// </summary>
    [CollectionDefinition(Name)]
    public class PackageStepTestCollection : ICollectionFixture<MSBuildRegistrationFixture>
    {
        public const string Name = "Package Step Tests";
    }
}
