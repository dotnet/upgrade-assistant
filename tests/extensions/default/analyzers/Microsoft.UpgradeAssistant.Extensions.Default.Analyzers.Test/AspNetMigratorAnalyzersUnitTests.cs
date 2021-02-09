using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UpgradeAssistant.MSBuild;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject;

namespace AspNetMigrator.Analyzers.Test
{
    [TestClass]
    public class AspNetMigratorAnalyzersUnitTests
    {
        private static readonly Dictionary<string, ExpectedDiagnostic[]> ExpectedDiagnostics = new()
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
                    new ExpectedDiagnostic("AM0004", new TextSpan(97, 13)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(171, 21)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(336, 22)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(402, 22)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(434, 22)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(624, 21)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(716, 22)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(866, 13)),
                    new ExpectedDiagnostic("AM0004", new TextSpan(941, 21))
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
            {
                "AM0007",
                new[]
                {
                    new ExpectedDiagnostic("AM0007", new TextSpan(131, 10)),
                    new ExpectedDiagnostic("AM0007", new TextSpan(314, 10)),
                    new ExpectedDiagnostic("AM0007", new TextSpan(377, 10)),
                    new ExpectedDiagnostic("AM0007", new TextSpan(417, 10)),
                    new ExpectedDiagnostic("AM0007", new TextSpan(597, 10)),
                    new ExpectedDiagnostic("AM0007", new TextSpan(617, 10)),
                    new ExpectedDiagnostic("AM0007", new TextSpan(675, 10)),
                    new ExpectedDiagnostic("AM0007", new TextSpan(735, 10)),
                    new ExpectedDiagnostic("AM0007", new TextSpan(826, 10)),
                    new ExpectedDiagnostic("AM0007", new TextSpan(871, 10)),
                }
            },
            {
                "AM0008",
                new[]
                {
                    new ExpectedDiagnostic("AM0008", new TextSpan(79, 9)),
                    new ExpectedDiagnostic("AM0008", new TextSpan(112, 9)),
                    new ExpectedDiagnostic("AM0008", new TextSpan(150, 9)),
                    new ExpectedDiagnostic("AM0008", new TextSpan(191, 9)),
                    new ExpectedDiagnostic("AM0008", new TextSpan(287, 9)),
                    new ExpectedDiagnostic("AM0008", new TextSpan(331, 9)),
                }
            },
            {
                "AM0009",
                new[]
                {
                    new ExpectedDiagnostic("AM0009", new TextSpan(102, 12)),
                    new ExpectedDiagnostic("AM0009", new TextSpan(143, 12)),
                    new ExpectedDiagnostic("AM0009", new TextSpan(216, 12)),
                    new ExpectedDiagnostic("AM0009", new TextSpan(243, 12)),
                    new ExpectedDiagnostic("AM0009", new TextSpan(311, 12)),
                }
            },
            {
                "AM0010",
                new[]
                {
                    new ExpectedDiagnostic("AM0010", new TextSpan(150, 9)),
                    new ExpectedDiagnostic("AM0010", new TextSpan(240, 18)),
                    new ExpectedDiagnostic("AM0010", new TextSpan(344, 13)),
                }
            },
        };

        [AssemblyInitialize]
#pragma warning disable IDE0060 // Remove unused parameter (required by MSTest)
        public static void Initialize(TestContext context)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            // Register MSBuild
            var msBuildRegistrar = new MSBuildRegistrationStartup(new NullLogger<MSBuildRegistrationStartup>());
            msBuildRegistrar.RegisterMSBuildInstance();

            // Make sure the TestProject's dependencies are restored
            RestoreTestProjectPackages();
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // MSBuild resolver must be registered before this is JIT'd
        private static void RestoreTestProjectPackages()
        {
            var restorer = new MSBuildPackageRestorer(new NullLogger<MSBuildPackageRestorer>());
            restorer.RestorePackages(new ProjectInstance(ProjectRootElement.Open(TestHelper.TestProjectPath)));
        }

        // No diagnostics expected to show up
        [TestMethod]
        public async Task NegativeTest()
        {
            var diagnostics = await TestHelper.GetDiagnosticsAsync("Startup.cs", TestHelper.AllAnalyzers
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
        [DataRow("AM0007")]
        [DataRow("AM0008")]
        [DataRow("AM0009")]
        [DataRow("AM0010")]
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
        [DataRow("AM0007")]
        [DataRow("AM0008")]
        [DataRow("AM0009")]
        [DataRow("AM0010")]
        [DataTestMethod]
        public async Task MigrationCodeFixer(string diagnosticId)
        {
            var fixedSource = await TestHelper.FixSourceAsync($"{diagnosticId}.cs", diagnosticId).ConfigureAwait(false);
            var expectedSource = await TestHelper.GetSourceAsync($"{diagnosticId}.Fixed.cs").ConfigureAwait(false);

            Assert.IsNotNull(expectedSource);

            var expectedText = (await expectedSource!.GetTextAsync().ConfigureAwait(false)).ToString();
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
