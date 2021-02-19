// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Fixtures;
using Xunit;

namespace Integration.Tests
{
    /// <summary>
    /// Class (which is never instantiated) for collecting integration
    /// tests that need integration test fixtures like the TryConvertFixture.
    /// </summary>
    [CollectionDefinition(Name)]
    public class IntegrationTestCollection : ICollectionFixture<TryConvertFixture>
    {
        public const string Name = "Integration Tests";
    }
}
