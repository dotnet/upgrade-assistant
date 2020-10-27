using AspNetMigrator.MSBuild;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using TestProject;

namespace AspNetMigrator.Analyzers.Test
{
    [TestClass]
    public class AspNetMigratorAnalyzersUnitTest
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext _)
        {
            MSBuildHelper.RegisterMSBuildInstance();
        }

        //No diagnostics expected to show up
        [TestMethod]
        public async Task NegativeTest()
        {
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new UsingSystemWebAnalyzer(), new HtmlStringAnalyzer());
            var diagnostics = await TestHelper.GetDiagnosticsAsync("Startup.cs", analyzers).ConfigureAwait(false);

            Assert.AreEqual(0, diagnostics.Count());
        }

        [TestMethod]
        public async Task AM0001()
        {
            var diagnosticId = "AM0001";
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new UsingSystemWebAnalyzer());
            var diagnostics = await TestHelper.GetDiagnosticsAsync($"{diagnosticId}.cs", analyzers).ConfigureAwait(false);

            var expectedDiagnostics = new[]
            {
                new ExpectedDiagnostic(diagnosticId, new TextSpan(15, 17)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(34, 23)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(59, 37)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(184, 11))
            };

            AssertDiagnosticsCorrect(diagnostics, expectedDiagnostics);
        }

        [TestMethod]
        public async Task AM0001CodeFix()
        {
            var diagnosticId = "AM0001";
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new UsingSystemWebAnalyzer());

            var fixedSource = await TestHelper.FixSourceAsync($"{diagnosticId}.cs", analyzers).ConfigureAwait(false);
            var expectedSource = await TestHelper.GetSourceAsync($"{diagnosticId}.Fixed.cs").ConfigureAwait(false);

            var expectedText = (await expectedSource.GetTextAsync().ConfigureAwait(false)).ToString();
            var fixedText = (await fixedSource.GetTextAsync().ConfigureAwait(false)).ToString();
            Assert.AreEqual(expectedText, fixedText);
        }

        [TestMethod]
        public async Task AM0002()
        {
            var diagnosticId = "AM0002";
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new HtmlStringAnalyzer());
            var diagnostics = await TestHelper.GetDiagnosticsAsync($"{diagnosticId}.cs", analyzers).ConfigureAwait(false);

            var expectedDiagnostics = new[]
            {
                new ExpectedDiagnostic(diagnosticId, new TextSpan(121, 11)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(171, 10)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(307, 10)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(375, 13)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(434, 13)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(486, 13))
            };

            AssertDiagnosticsCorrect(diagnostics, expectedDiagnostics);
        }

        [TestMethod]
        public async Task AM0002CodeFix()
        {
            var diagnosticId = "AM0002";
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new HtmlStringAnalyzer());

            var fixedSource = await TestHelper.FixSourceAsync($"{diagnosticId}.cs", analyzers).ConfigureAwait(false);
            var expectedSource = await TestHelper.GetSourceAsync($"{diagnosticId}.Fixed.cs").ConfigureAwait(false);

            var expectedText = (await expectedSource.GetTextAsync().ConfigureAwait(false)).ToString();
            var fixedText = (await fixedSource.GetTextAsync().ConfigureAwait(false)).ToString();
            Assert.AreEqual(expectedText, fixedText);
        }

        [TestMethod]
        public async Task AM0003()
        {
            var diagnosticId = "AM0003";
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new ResultTypeAnalyzer());
            var diagnostics = await TestHelper.GetDiagnosticsAsync($"{diagnosticId}.cs", analyzers).ConfigureAwait(false);

            var expectedDiagnostics = new[]
            {
                new ExpectedDiagnostic(diagnosticId, new TextSpan(248, 10)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(339, 14)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(375, 14)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(416, 12)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(485, 12)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(521, 10)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(556, 14)),
                new ExpectedDiagnostic(diagnosticId, new TextSpan(605, 18))
            };

            AssertDiagnosticsCorrect(diagnostics, expectedDiagnostics);
        }

        [TestMethod]
        public async Task AM0003CodeFix()
        {
            var diagnosticId = "AM0003";
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new ResultTypeAnalyzer());

            var fixedSource = await TestHelper.FixSourceAsync($"{diagnosticId}.cs", analyzers).ConfigureAwait(false);
            var expectedSource = await TestHelper.GetSourceAsync($"{diagnosticId}.Fixed.cs").ConfigureAwait(false);

            var expectedText = (await expectedSource.GetTextAsync().ConfigureAwait(false)).ToString();
            var fixedText = (await fixedSource.GetTextAsync().ConfigureAwait(false)).ToString();
            Assert.AreEqual(expectedText, fixedText);
        }

        private static void AssertDiagnosticsCorrect(IEnumerable<Diagnostic> diagnostics, ExpectedDiagnostic[] expectedDiagnostics)
        {
            Assert.AreEqual(expectedDiagnostics.Length, diagnostics.Count());
            var count = 0;
            foreach (var d in diagnostics.OrderBy(d => d.Location.SourceSpan.Start))
            {
                Assert.IsTrue(expectedDiagnostics[count++].Equals(d), $"Expected diagnostic {count} to be at {expectedDiagnostics[count - 1].SourceSpan}; actually at {d.Location.SourceSpan}");
            }
        }
    }
}
