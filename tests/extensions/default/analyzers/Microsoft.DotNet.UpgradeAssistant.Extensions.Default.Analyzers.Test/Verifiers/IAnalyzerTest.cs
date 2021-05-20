// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Testing;

#pragma warning disable CA1002 // Do not expose generic lists

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public interface IAnalyzerTest
    {
        CompilerDiagnostics CompilerDiagnostics { get; set; }

        List<string> DisabledDiagnostics { get; }

        List<DiagnosticResult> ExpectedDiagnostics { get; }

        string Language { get; }

        MarkupOptions MarkupOptions { get; set; }

        List<Func<OptionSet, OptionSet>> OptionsTransforms { get; }

        ReferenceAssemblies ReferenceAssemblies { get; set; }

        List<Func<Solution, ProjectId, Solution>> SolutionTransforms { get; }

        TestBehaviors TestBehaviors { get; set; }

        SolutionState TestState { get; }

        Dictionary<string, string> XmlReferences { get; }

        Task RunAsync(CancellationToken cancellationToken = default);
    }
}
