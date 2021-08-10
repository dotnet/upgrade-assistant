// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source.Tests
{
    /// <summary>
    /// Class (which is never instantiated) for diagnosticeanalysisrunner tests that
    /// need test fixtures to register MSBuild and restore test project packages.
    /// </summary>
    [CollectionDefinition(Name)]
    public class DiagnosticAnalyzerTestsCollection : ICollectionFixture<RestoreTestProjectFixture>
    {
        public const string Name = "DiagnosticAnalyzerRunner Tests";
    }
}
