// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public interface ICodeFixTest : IAnalyzerTest
    {
        SolutionState BatchFixedState { get; }

        CodeFixTestBehaviors CodeFixTestBehaviors { get; set; }

        CodeActionValidationMode CodeActionValidationMode { get; set; }

        SolutionState FixedState { get; }

        int? NumberOfFixAllInDocumentIterations { get; set; }

        int? NumberOfFixAllIterations { get; set; }

        int? NumberOfIncrementalIterations { get; set; }
    }
}
