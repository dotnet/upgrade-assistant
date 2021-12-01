// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public class AnalyzerTests
    {
        private readonly ITestOutputHelper _output;

        public AnalyzerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static readonly Dictionary<string, ExpectedDiagnostic[]> ExpectedDiagnostics = new()
        {
            // Using System.Web scenarios
            {
                "UA0001",
                new[]
                {
                    new ExpectedDiagnostic("UA0001", new TextSpan(15, 17)),
                    new ExpectedDiagnostic("UA0001", new TextSpan(34, 23)),
                    new ExpectedDiagnostic("UA0001", new TextSpan(59, 37)),
                    new ExpectedDiagnostic("UA0001", new TextSpan(184, 11))
                }
            },

            // HttpContext.Current scenarios
            {
                "UA0005",
                new[]
                {
                    new ExpectedDiagnostic("UA0005", new TextSpan(241, 19)),
                    new ExpectedDiagnostic("UA0005", new TextSpan(385, 23)),
                    new ExpectedDiagnostic("UA0005", new TextSpan(431, 30)),
                    new ExpectedDiagnostic("UA0005", new TextSpan(494, 19))
                }
            },

            // HttpContext.IsDebuggingEnabled scenarios
            {
                "UA0006",
                new[]
                {
                    new ExpectedDiagnostic("UA0006", new TextSpan(155, 38)),
                    new ExpectedDiagnostic("UA0006", new TextSpan(300, 20)),
                    new ExpectedDiagnostic("UA0006", new TextSpan(403, 44)),
                    new ExpectedDiagnostic("UA0006", new TextSpan(497, 42))
                }
            },

            // HtmlHelper scenarios
            {
                "UA0007",
                new[]
                {
                    new ExpectedDiagnostic("UA0007", new TextSpan(116, 25)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(310, 14)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(362, 25)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(402, 25)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(593, 14)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(617, 10)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(660, 25)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(735, 10)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(811, 25)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(871, 10)),
                }
            },

            // IUrlHelper scenarios
            {
                "UA0008",
                new[]
                {
                    new ExpectedDiagnostic("UA0008", new TextSpan(64, 24)),
                    new ExpectedDiagnostic("UA0008", new TextSpan(112, 9)),
                    new ExpectedDiagnostic("UA0008", new TextSpan(135, 24)),
                    new ExpectedDiagnostic("UA0008", new TextSpan(187, 13)),
                    new ExpectedDiagnostic("UA0008", new TextSpan(287, 9)),
                    new ExpectedDiagnostic("UA0008", new TextSpan(316, 24)),
                }
            },

            // AllowHtmlAttribute scenarios
            {
                "UA0010",
                new[]
                {
                    new ExpectedDiagnostic("UA0010", new TextSpan(150, 9)),
                    new ExpectedDiagnostic("UA0010", new TextSpan(240, 18)),
                    new ExpectedDiagnostic("UA0010", new TextSpan(426, 24)),
                }
            },

            // UnsafeDeserialize.UnsafeDeserialize scenarios
            {
                "UA0012",
                new[]
                {
                    new ExpectedDiagnostic("UA0012", new TextSpan(2256, 28)),
                    new ExpectedDiagnostic("UA0012", new TextSpan(3169, 28)),
                    new ExpectedDiagnostic("UA0012", new TextSpan(4096, 39)),
                    new ExpectedDiagnostic("UA0012", new TextSpan(4950, 39)),
                    new ExpectedDiagnostic("UA0012", new TextSpan(2287, 28), Language.VisualBasic),
                    new ExpectedDiagnostic("UA0012", new TextSpan(3212, 28), Language.VisualBasic),
                    new ExpectedDiagnostic("UA0012", new TextSpan(4287, 39), Language.VisualBasic),
                    new ExpectedDiagnostic("UA0012", new TextSpan(5150, 39), Language.VisualBasic),
                }
            },

            // TypeUpgradeAnalyzer scenarios
            {
                "HelperResultUpgrade",
                new[]
                {
                    new ExpectedDiagnostic("UA0002", new TextSpan(115, 12)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(136, 32)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(209, 32)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(256, 12)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(320, 16)),
                }
            },
            {
                "HtmlStringUpgrade",
                new[]
                {
                    new ExpectedDiagnostic("UA0002", new TextSpan(132, 11)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(182, 10)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(307, 21)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(371, 28)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(445, 13)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(497, 13))
                }
            },
            {
                "ResultUpgrade",
                new[]
                {
                    new ExpectedDiagnostic("UA0002", new TextSpan(240, 25)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(342, 18)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(382, 14)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(423, 12)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(477, 27)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(513, 25)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(563, 14)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(612, 18))
                }
            },
            {
                "FilterUpgrade",
                new[]
                {
                    new ExpectedDiagnostic("UA0002", new TextSpan(88, 28)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(162, 36)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(327, 37)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(404, 26)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(436, 26)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(615, 36)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(707, 37)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(872, 13)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(947, 21))
                }
            },
            {
                "ControllerUpgrade",
                new[]
                {
                    new ExpectedDiagnostic("UA0002", new TextSpan(187, 13)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(615, 29)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(954, 14)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(1030, 25)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(1079, 10)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(1102, 13)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(1160, 25)),

                    new ExpectedDiagnostic("UA0002", new TextSpan(177, 13), Language.VisualBasic),
                    new ExpectedDiagnostic("UA0002", new TextSpan(463, 29), Language.VisualBasic),
                    new ExpectedDiagnostic("UA0002", new TextSpan(982, 10), Language.VisualBasic),
                    new ExpectedDiagnostic("UA0002", new TextSpan(1265, 25), Language.VisualBasic),
                }
            },
            {
                "AttributesTest",
                new[]
                {
                    new ExpectedDiagnostic("UA0010", new TextSpan(370, 9)),
                    new ExpectedDiagnostic("UA0010", new TextSpan(458, 20)),
                    new ExpectedDiagnostic("UA0010", new TextSpan(527, 21)),
                    new ExpectedDiagnostic("UA0010", new TextSpan(549, 33)),
                    new ExpectedDiagnostic("UA0010", new TextSpan(684, 24)),
                    new ExpectedDiagnostic("UA0010", new TextSpan(716, 22)),
                    new ExpectedDiagnostic("UA0010", new TextSpan(782, 4)),
                    new ExpectedDiagnostic("UA0010", new TextSpan(874, 13)),

                    new ExpectedDiagnostic("UA0010", new TextSpan(295, 11), Language.VisualBasic),
                    new ExpectedDiagnostic("UA0010", new TextSpan(392, 22), Language.VisualBasic),
                    new ExpectedDiagnostic("UA0010", new TextSpan(475, 23), Language.VisualBasic),
                    new ExpectedDiagnostic("UA0010", new TextSpan(500, 33), Language.VisualBasic),
                    new ExpectedDiagnostic("UA0010", new TextSpan(638, 26), Language.VisualBasic),
                    new ExpectedDiagnostic("UA0010", new TextSpan(672, 24), Language.VisualBasic),
                    new ExpectedDiagnostic("UA0010", new TextSpan(733, 6), Language.VisualBasic),
                    new ExpectedDiagnostic("UA0010", new TextSpan(845, 13), Language.VisualBasic),
                }
            },
            {
                "ApiAlert",
                new[]
                {
                    new ExpectedDiagnostic("UA0013_D", new TextSpan(272, 29)),
                    new ExpectedDiagnostic("UA0013_D", new TextSpan(310, 29)),
                    new ExpectedDiagnostic("UA0013_A", new TextSpan(534, 11)),
                    new ExpectedDiagnostic("UA0013_A", new TextSpan(547, 23)),
                    new ExpectedDiagnostic("UA0013_D", new TextSpan(604, 29)),
                    new ExpectedDiagnostic("UA0013_D", new TextSpan(848, 29)),
                    new ExpectedDiagnostic("UA0013_D", new TextSpan(916, 29)),
                    new ExpectedDiagnostic("UA0013_E", new TextSpan(987, 15)),
                    new ExpectedDiagnostic("UA0013_B", new TextSpan(1046, 15)),
                    new ExpectedDiagnostic("UA0013_E", new TextSpan(1091, 39)),
                    new ExpectedDiagnostic("UA0013_E", new TextSpan(1139, 28)),
                    new ExpectedDiagnostic("UA0013_B", new TextSpan(1184, 11)),
                    new ExpectedDiagnostic("UA0013_C", new TextSpan(1249, 16)),
                    new ExpectedDiagnostic("UA0013_C", new TextSpan(1270, 33)),
                    new ExpectedDiagnostic("UA0013_G", new TextSpan(1354, 27)),
                    new ExpectedDiagnostic("UA0013_F", new TextSpan(1477, 23)),
                    new ExpectedDiagnostic("UA0013_F", new TextSpan(1568, 39)),

                    // Once for the namespace, once for the type
                    new ExpectedDiagnostic("UA0013_G", new TextSpan(1658, 25)),
                    new ExpectedDiagnostic("UA0013_G", new TextSpan(1658, 60)),

                    new ExpectedDiagnostic("UA0013_H", new TextSpan(2057, 15)),
                    new ExpectedDiagnostic("UA0013_H", new TextSpan(2131, 29)),
                }
            },
        };

        // No diagnostics expected to show up
        [Fact]
        public async Task NegativeTest()
        {
            using var workspace = new AdhocWorkspace();
            var analyzers = TestHelper.AllAnalyzers
                .SelectMany(a => a.SupportedDiagnostics)
                .Select(d => d.Id)
                .ToArray();
            var diagnostics = await workspace.GetDiagnosticsAsync("Startup.cs", analyzers, isFramework: true).ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [InlineData("UA0001")]
        [InlineData("HtmlStringUpgrade")]
        [InlineData("ResultUpgrade")]
        [InlineData("FilterUpgrade")]
        [InlineData("UA0005")]
        [InlineData("UA0006")]
        [InlineData("UA0007")]
        [InlineData("UA0008")]
        [InlineData("HelperResultUpgrade")]
        [InlineData("UA0010")]
        [InlineData("UA0012")]
        [InlineData("ControllerUpgrade")]
        [InlineData("AttributesTest")]
        [InlineData("ApiAlert")]
        [Theory]
        public async Task UpgradeAnalyzers(string scenarioName)
        {
            foreach (var language in new[] { Language.CSharp, Language.VisualBasic })
            {
                var expectedDiagnostics = ExpectedDiagnostics[scenarioName]
                    .Where(diagnostics => diagnostics.Language == language);
                if (!expectedDiagnostics.Any())
                {
                    // nothing to see here, move along
                    continue;
                }

                var expectedDiagnosticIds = expectedDiagnostics
                    .Select(e => e.Id)
                    .Distinct();

                using var workspace = new AdhocWorkspace();
                var diagnostics = await workspace.GetDiagnosticsAsync(language, scenarioName, isFramework: false, expectedDiagnosticIds);
                AssertDiagnosticsCorrect(diagnostics, expectedDiagnostics);
            }
        }

        [InlineData("UA0001")]
        [InlineData("HtmlStringUpgrade")]
        [InlineData("ResultUpgrade")]
        [InlineData("FilterUpgrade")]
        [InlineData("UA0005")]
        [InlineData("UA0006")]
        [InlineData("UA0007")]
        [InlineData("UA0008")]
        [InlineData("HelperResultUpgrade")]
        [InlineData("UA0010")]
        [InlineData("UA0012")]
        [InlineData("ControllerUpgrade")]
        [InlineData("AttributesTest")]
        [Theory]
        public async Task UpgradeCodeFixer(string scenarioName)
        {
            foreach (var language in new[] { Language.CSharp, Language.VisualBasic })
            {
                var expectedDiagnostics = ExpectedDiagnostics[scenarioName].Where(diagnostics => diagnostics.Language == language);
                if (!expectedDiagnostics.Any())
                {
                    // nothing to see here, move along
                    continue;
                }

                using var workspace = new AdhocWorkspace();
                var fixedText = await workspace.FixSourceAsync(language, scenarioName, expectedDiagnostics.Select(d => d.Id).Distinct()).ConfigureAwait(false);
                var expectedText = TestHelper.GetSource(language, $"{scenarioName}.Fixed");

                _output.WriteLine("Expected:");
                _output.WriteLine(expectedText);
                _output.WriteLine("Actual:");
                _output.WriteLine(fixedText);

                Assert.Equal(expectedText, fixedText);
            }
        }

        private static void AssertDiagnosticsCorrect(IEnumerable<Diagnostic> diagnostics, IEnumerable<ExpectedDiagnostic> expectedDiagnostics)
        {
            Assert.Equal(expectedDiagnostics.Count(), diagnostics.Count());

            var count = 0;
            foreach (var d in diagnostics.OrderBy(d => d.Location.SourceSpan.Start))
            {
                var expected = $"{expectedDiagnostics.ElementAt(count).SourceSpan}";
                var actual = $"{d.Location.SourceSpan}";
                Assert.True(expectedDiagnostics.ElementAt(count).Matches(d), $"Expected {expectedDiagnostics.ElementAt(count).Language} diagnostic {count} to be at {expectedDiagnostics.ElementAt(count).SourceSpan}; actually at {d.Location.SourceSpan}");
                count++;
            }
        }
    }
}
