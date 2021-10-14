// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public static class TestExtensions
    {
        public static TTest WithSource<TTest>(this TTest test, string source)
            where TTest : IAnalyzerTest
        {
            test.TestState.Sources.Add(source);
            return test;
        }

        public static TTest WithSystemWeb<TTest>(this TTest test)
            where TTest : IAnalyzerTest
        {
            test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net45.Default.AddAssemblies(ImmutableArray.Create("System.Web"));
            return test;
        }

        public static TTest WithFixed<TTest>(this TTest test, string fixedCode, string? name = null)
            where TTest : ICodeFixTest
        {
            if (name is not null)
            {
                test.FixedState.Sources.Add((name, fixedCode));
            }
            else
            {
                test.FixedState.Sources.Add(fixedCode);
            }

            return test;
        }

        public static TTest WithExpectedDiagnostics<TTest>(this TTest test, params DiagnosticResult[] expected)
            where TTest : IAnalyzerTest
        {
            test.ExpectedDiagnostics.AddRange(expected);
            return test;
        }

        public static TTest WithExpectedDiagnosticsAfter<TTest>(this TTest test, params DiagnosticResult[] expected)
            where TTest : ICodeFixTest
        {
            test.FixedState.ExpectedDiagnostics.AddRange(expected);
            return test;
        }

        public static TTest With<TTest>(this TTest test, CodeFixTestBehaviors behavior)
            where TTest : ICodeFixTest
        {
            test.CodeFixTestBehaviors = behavior;
            return test;
        }

        public static TTest With<TTest>(this TTest test, CodeActionValidationMode mode)
            where TTest : ICodeFixTest
        {
            test.CodeActionValidationMode = mode;
            return test;
        }

        public static TTest ExpectedAfter<TTest>(this TTest test, params DiagnosticResult[] expectedAfter)
            where TTest : ICodeFixTest
        {
            if (expectedAfter is not null)
            {
                test.FixedState.ExpectedDiagnostics.AddRange(expectedAfter);
            }

            return test;
        }
    }
}
