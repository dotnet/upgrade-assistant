// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers.Test
{
    [Collection(AnalyzerTestCollection.Name)]
    public class AnalyzerTests
    {
        private static readonly Dictionary<string, ExpectedDiagnostic[]> ExpectedDiagnostics = new()
        {
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
            {
                "UA0002",
                new[]
                {
                    new ExpectedDiagnostic("UA0002", new TextSpan(121, 11)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(171, 10)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(307, 10)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(375, 13)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(434, 13)),
                    new ExpectedDiagnostic("UA0002", new TextSpan(486, 13))
                }
            },
            {
                "UA0003",
                new[]
                {
                    new ExpectedDiagnostic("UA0003", new TextSpan(248, 10)),
                    new ExpectedDiagnostic("UA0003", new TextSpan(339, 14)),
                    new ExpectedDiagnostic("UA0003", new TextSpan(375, 14)),
                    new ExpectedDiagnostic("UA0003", new TextSpan(416, 12)),
                    new ExpectedDiagnostic("UA0003", new TextSpan(485, 12)),
                    new ExpectedDiagnostic("UA0003", new TextSpan(521, 10)),
                    new ExpectedDiagnostic("UA0003", new TextSpan(556, 14)),
                    new ExpectedDiagnostic("UA0003", new TextSpan(605, 18))
                }
            },
            {
                "UA0004",
                new[]
                {
                    new ExpectedDiagnostic("UA0004", new TextSpan(97, 13)),
                    new ExpectedDiagnostic("UA0004", new TextSpan(171, 21)),
                    new ExpectedDiagnostic("UA0004", new TextSpan(336, 22)),
                    new ExpectedDiagnostic("UA0004", new TextSpan(402, 22)),
                    new ExpectedDiagnostic("UA0004", new TextSpan(434, 22)),
                    new ExpectedDiagnostic("UA0004", new TextSpan(624, 21)),
                    new ExpectedDiagnostic("UA0004", new TextSpan(716, 22)),
                    new ExpectedDiagnostic("UA0004", new TextSpan(866, 13)),
                    new ExpectedDiagnostic("UA0004", new TextSpan(941, 21))
                }
            },
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
            {
                "UA0007",
                new[]
                {
                    new ExpectedDiagnostic("UA0007", new TextSpan(131, 10)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(314, 10)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(377, 10)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(417, 10)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(597, 10)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(617, 10)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(675, 10)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(735, 10)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(826, 10)),
                    new ExpectedDiagnostic("UA0007", new TextSpan(871, 10)),
                }
            },
            {
                "UA0008",
                new[]
                {
                    new ExpectedDiagnostic("UA0008", new TextSpan(79, 9)),
                    new ExpectedDiagnostic("UA0008", new TextSpan(112, 9)),
                    new ExpectedDiagnostic("UA0008", new TextSpan(150, 9)),
                    new ExpectedDiagnostic("UA0008", new TextSpan(191, 9)),
                    new ExpectedDiagnostic("UA0008", new TextSpan(287, 9)),
                    new ExpectedDiagnostic("UA0008", new TextSpan(331, 9)),
                }
            },
            {
                "UA0009",
                new[]
                {
                    new ExpectedDiagnostic("UA0009", new TextSpan(102, 12)),
                    new ExpectedDiagnostic("UA0009", new TextSpan(143, 12)),
                    new ExpectedDiagnostic("UA0009", new TextSpan(216, 12)),
                    new ExpectedDiagnostic("UA0009", new TextSpan(243, 12)),
                    new ExpectedDiagnostic("UA0009", new TextSpan(311, 12)),
                }
            },
            {
                "UA0010",
                new[]
                {
                    new ExpectedDiagnostic("UA0010", new TextSpan(150, 9)),
                    new ExpectedDiagnostic("UA0010", new TextSpan(240, 18)),
                    new ExpectedDiagnostic("UA0010", new TextSpan(344, 13)),
                }
            },
            {
                "UA0012",
                new[]
                {
                    new ExpectedDiagnostic("UA0012", new TextSpan(2249, 28)),
                    new ExpectedDiagnostic("UA0012", new TextSpan(3162, 28)),
                    new ExpectedDiagnostic("UA0012", new TextSpan(4089, 39)),
                    new ExpectedDiagnostic("UA0012", new TextSpan(4943, 39)),
                }
            },
            {
                "UA0013",
                new[]
                {
                    new ExpectedDiagnostic("UA0013", new TextSpan(93, 13)),
                    new ExpectedDiagnostic("UA0013", new TextSpan(275, 29)),
                    new ExpectedDiagnostic("UA0013", new TextSpan(107, 13), Language.VisualBasic),
                    new ExpectedDiagnostic("UA0013", new TextSpan(330, 29), Language.VisualBasic),
                }
            },
        };

        // No diagnostics expected to show up
        [Fact]
        public async Task NegativeTest()
        {
            var diagnostics = await TestHelper.GetDiagnosticsAsync("Startup.cs", TestHelper.AllAnalyzers
                .SelectMany(a => a.SupportedDiagnostics)
                .Select(d => d.Id)
                .ToArray()).ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [InlineData("UA0001")]
        [InlineData("UA0002")]
        [InlineData("UA0003")]
        [InlineData("UA0004")]
        [InlineData("UA0005")]
        [InlineData("UA0006")]
        [InlineData("UA0007")]
        [InlineData("UA0008")]
        [InlineData("UA0009")]
        [InlineData("UA0010")]
        [InlineData("UA0012")]
        [InlineData("UA0013")]
        [Theory]
        public async Task UpgradeAnalyzers(string diagnosticId)
        {
            foreach (var language in new[] { Language.CSharp, Language.VisualBasic })
            {
                var expectedDiagnostics = ExpectedDiagnostics[diagnosticId].Where(diagnostics => diagnostics.Language == language);
                if (!expectedDiagnostics.Any())
                {
                    // nothing to see here, move along
                    continue;
                }

                var fileExtension = language.GetFileExtension();
                var diagnostics = await TestHelper.GetDiagnosticsAsync(language, $"{diagnosticId}.{fileExtension}", diagnosticId).ConfigureAwait(false);
                AssertDiagnosticsCorrect(diagnostics, expectedDiagnostics);
            }
        }

        [InlineData("UA0001")]
        [InlineData("UA0002")]
        [InlineData("UA0003")]
        [InlineData("UA0004")]
        [InlineData("UA0005")]
        [InlineData("UA0006")]
        [InlineData("UA0007")]
        [InlineData("UA0008")]
        [InlineData("UA0009")]
        [InlineData("UA0010")]
        [InlineData("UA0012")]
        [InlineData("UA0013")]
        [Theory]
        public async Task UpgradeCodeFixer(string diagnosticId)
        {
            var fixedSource = await TestHelper.FixSourceAsync($"{diagnosticId}.cs", diagnosticId).ConfigureAwait(false);
            var expectedSource = await TestHelper.GetSourceAsync($"{diagnosticId}.Fixed.cs").ConfigureAwait(false);

            Assert.NotNull(expectedSource);

            var expectedText = (await expectedSource!.GetTextAsync().ConfigureAwait(false)).ToString();
            var fixedText = (await fixedSource.GetTextAsync().ConfigureAwait(false)).ToString();
            Assert.Equal(expectedText, fixedText);
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
