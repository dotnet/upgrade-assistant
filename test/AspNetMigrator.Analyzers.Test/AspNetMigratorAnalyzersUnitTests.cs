using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AspNetMigrator.MSBuild;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject;

namespace AspNetMigrator.Analyzers.Test
{
    [TestClass]
    public class AspNetMigratorAnalyzersUnitTests
    {
        private static readonly Dictionary<string, ExpectedDiagnostic[]> ExpectedDiagnostics = new Dictionary<string, ExpectedDiagnostic[]>
        {
            {
                "AM0001",
                new[]
                {
                    new ExpectedDiagnostic("AM0001", new TextSpan(15, 17)),
                    new ExpectedDiagnostic("AM0001", new TextSpan(34, 23)),
                    new ExpectedDiagnostic("AM0001", new TextSpan(59, 37)),
                    new ExpectedDiagnostic("AM0001", new TextSpan(184, 11))
                }
            },
            {
                "AM0002",
                new[]
                {
                    new ExpectedDiagnostic("AM0002", new TextSpan(121, 11)),
                    new ExpectedDiagnostic("AM0002", new TextSpan(171, 10)),
                    new ExpectedDiagnostic("AM0002", new TextSpan(307, 10)),
                    new ExpectedDiagnostic("AM0002", new TextSpan(375, 13)),
                    new ExpectedDiagnostic("AM0002", new TextSpan(434, 13)),
                    new ExpectedDiagnostic("AM0002", new TextSpan(486, 13))
                }
            },
            {
                "AM0003",
                new[]
                {
                    new ExpectedDiagnostic("AM0003", new TextSpan(248, 10)),
                    new ExpectedDiagnostic("AM0003", new TextSpan(339, 14)),
                    new ExpectedDiagnostic("AM0003", new TextSpan(375, 14)),
                    new ExpectedDiagnostic("AM0003", new TextSpan(416, 12)),
                    new ExpectedDiagnostic("AM0003", new TextSpan(485, 12)),
                    new ExpectedDiagnostic("AM0003", new TextSpan(521, 10)),
                    new ExpectedDiagnostic("AM0003", new TextSpan(556, 14)),
                    new ExpectedDiagnostic("AM0003", new TextSpan(605, 18))
                }
            },
            {
                "AM0004",
                new[]
                {
                    new ExpectedDiagnostic("AM0004", new TextSpan(105, 13)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(164, 21)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(314, 22)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(380, 22)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(412, 22)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(443, 22)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(567, 21)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(644, 22)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(794, 13)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(869, 21))
                }
            },
            {
                "AM0005",
                new[]
                {
                    new ExpectedDiagnostic("AM0005", new TextSpan(241, 19)),
                    new ExpectedDiagnostic("AM0005", new TextSpan(385, 23)),
                    new ExpectedDiagnostic("AM0005", new TextSpan(431, 30)),
                    new ExpectedDiagnostic("AM0005", new TextSpan(494, 19))
                }
            },
            {
                "AM0006",
                new[]
                {
                    new ExpectedDiagnostic("AM0006", new TextSpan(157, 38)),
                    new ExpectedDiagnostic("AM0006", new TextSpan(302, 20)),
                    new ExpectedDiagnostic("AM0006", new TextSpan(405, 44)),
                    new ExpectedDiagnostic("AM0006", new TextSpan(499, 42))
                }
            },
        };

        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            MSBuildHelper.RegisterMSBuildInstance();
        }

        // No diagnostics expected to show up
        [TestMethod]
        public async Task NegativeTest()
        {
            var diagnostics = await TestHelper.GetDiagnosticsAsync("Startup.cs", AspNetCoreMigrationAnalyzers.AllAnalyzers
                .SelectMany(a => a.SupportedDiagnostics)
                .Select(d => d.Id)
                .ToArray()).ConfigureAwait(false);

            Assert.AreEqual(0, diagnostics.Count());
        }

        [DataRow("AM0001")]
        [DataRow("AM0002")]
        [DataRow("AM0003")]
        [DataRow("AM0004")]
        [DataRow("AM0005")]
        [DataRow("AM0006")]
        [DataTestMethod]
        public async Task MigrationAnalyzers(string diagnosticId)
        {
            var diagnostics = await TestHelper.GetDiagnosticsAsync($"{diagnosticId}.cs", diagnosticId).ConfigureAwait(false);

            AssertDiagnosticsCorrect(diagnostics, ExpectedDiagnostics[diagnosticId]);
        }

        [DataRow("AM0001")]
        [DataRow("AM0002")]
        [DataRow("AM0003")]
        [DataRow("AM0004")]
        [DataRow("AM0005")]
        [DataRow("AM0006")]
        [DataTestMethod]
        public async Task MigrationCodeFixer(string diagnosticId)
        {
            var fixedSource = await TestHelper.FixSourceAsync($"{diagnosticId}.cs", diagnosticId).ConfigureAwait(false);
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
                Assert.IsTrue(expectedDiagnostics[count++].Matches(d), $"Expected diagnostic {count} to be at {expectedDiagnostics[count - 1].SourceSpan}; actually at {d.Location.SourceSpan}");
            }
        }
    }
}
