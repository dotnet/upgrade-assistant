// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public static class TestExtensions
    {
        public static AnalyzerTest<TVerifier> WithSource<TVerifier>(this AnalyzerTest<TVerifier> test, string source)
            where TVerifier : IVerifier, new()
        {
            test.TestState.Sources.Add(source);
            return test;
        }

        public static CodeFixTest<TVerifier> WithSource<TVerifier>(this CodeFixTest<TVerifier> test, string source)
            where TVerifier : IVerifier, new()
        {
            test.TestState.Sources.Add(source);
            return test;
        }

        public static AnalyzerTest<TVerifier> WithSystemWeb<TVerifier>(this AnalyzerTest<TVerifier> test)
            where TVerifier : IVerifier, new()
        {
            test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net45.Default.AddAssemblies(ImmutableArray.Create("System.Web"));
            return test;
        }

        public static CodeFixTest<TVerifier> WithSystemWeb<TVerifier>(this CodeFixTest<TVerifier> test)
            where TVerifier : IVerifier, new()
        {
            test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net45.Default.AddAssemblies(ImmutableArray.Create("System.Web"));
            return test;
        }

        public static CodeFixTest<TVerifier> WithFixed<TVerifier>(this CodeFixTest<TVerifier> test, string fixedCode)
            where TVerifier : IVerifier, new()
        {
            test.FixedState.Sources.Add(fixedCode);
            return test;
        }

        public static AnalyzerTest<TVerifier> WithExpectedDiagnostics<TVerifier>(this AnalyzerTest<TVerifier> test, params DiagnosticResult[] expected)
            where TVerifier : IVerifier, new()
        {
            test.ExpectedDiagnostics.AddRange(expected);
            return test;
        }

        public static CodeFixTest<TVerifier> WithExpectedDiagnostics<TVerifier>(this CodeFixTest<TVerifier> test, params DiagnosticResult[] expected)
            where TVerifier : IVerifier, new()
        {
            test.ExpectedDiagnostics.AddRange(expected);
            return test;
        }

        public static CodeFixTest<TVerifier> WithExpectedDiagnosticsAfter<TVerifier>(this CodeFixTest<TVerifier> test, params DiagnosticResult[] expected)
            where TVerifier : IVerifier, new()
        {
            test.FixedState.ExpectedDiagnostics.AddRange(expected);
            return test;
        }

        public static CodeFixTest<TVerifier> With<TVerifier>(this CodeFixTest<TVerifier> test, CodeFixTestBehaviors behavior)
            where TVerifier : IVerifier, new()
        {
            test.CodeFixTestBehaviors = behavior;
            return test;
        }

        public static CodeFixTest<TVerifier> With<TVerifier>(this CodeFixTest<TVerifier> test, CodeActionValidationMode mode)
            where TVerifier : IVerifier, new()
        {
            test.CodeActionValidationMode = mode;
            return test;
        }

        public static CodeFixTest<TVerifier> ExpectedAfter<TVerifier>(this CodeFixTest<TVerifier> test, params DiagnosticResult[] expectedAfter)
            where TVerifier : IVerifier, new()
        {
            if (expectedAfter is not null)
            {
                test.FixedState.ExpectedDiagnostics.AddRange(expectedAfter);
            }

            return test;
        }
    }
}
