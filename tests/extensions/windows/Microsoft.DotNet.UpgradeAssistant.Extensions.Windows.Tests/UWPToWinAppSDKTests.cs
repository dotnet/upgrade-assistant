﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using AutoFixture;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.Tests
{
    public class UWPToWinAppSDKTests
    {
        private static readonly Dictionary<string, ExpectedDiagnostic[]> ExpectedDiagnostics = new Dictionary<string, ExpectedDiagnostic[]>()
        {
            {
                "ContentDialogCaller",
                new[]
                {
                    new ExpectedDiagnostic(WinUIContentDialogAnalyzer.DiagnosticId, new TextSpan(729, 20), new TextSpan(707, 20))
                }
            },
            {
                "InitializeWithWindow",
                new[]
                {
                    new ExpectedDiagnostic(WinUIInitializeWindowAnalyzer.DiagnosticId, new TextSpan(415, 20), new TextSpan(399, 20)),
                    new ExpectedDiagnostic(WinUIInitializeWindowAnalyzer.DiagnosticId, new TextSpan(469, 18), new TextSpan(452, 18))
                }
            },
            {
                "DataTransferManagerCaller",
                new[]
                {
                    new ExpectedDiagnostic(WinUIDataTransferManagerAnalyzer.DiagnosticId, new TextSpan(413, 33), new TextSpan(397, 33)),
                    new ExpectedDiagnostic(WinUIDataTransferManagerAnalyzer.DiagnosticId, new TextSpan(461, 53), new TextSpan(444, 53)),
                    new ExpectedDiagnostic(WinUIDataTransferManagerAnalyzer.DiagnosticId, new TextSpan(529, 71), new TextSpan(511, 71))
                }
            },
            {
                "InteropsCaller",
                new[]
                {
                    new ExpectedDiagnostic(WinUIInteropAnalyzer.DiagnosticId, new TextSpan(481, 60), new TextSpan(464, 60)),
                    new ExpectedDiagnostic(WinUIInteropAnalyzer.DiagnosticId, new TextSpan(570, 92), new TextSpan(552, 92)),
                    new ExpectedDiagnostic(WinUIInteropAnalyzer.DiagnosticId, new TextSpan(746, 39), new TextSpan(723, 39)),
                    new ExpectedDiagnostic(WinUIInteropAnalyzer.DiagnosticId, new TextSpan(808, 91), new TextSpan(784, 91))
                }
            },
            {
                "MRTResourceManagerCaller",
                new[]
                {
                    new ExpectedDiagnostic(WinUIMRTResourceManagerAnalyzer.ResourceManagerAPIDiagnosticId, new TextSpan(452, 23), new TextSpan(436, 23)),
                    new ExpectedDiagnostic(WinUIMRTResourceManagerAnalyzer.ResourceManagerAPIDiagnosticId, new TextSpan(520, 68), new TextSpan(503, 68)),
                    new ExpectedDiagnostic(WinUIMRTResourceManagerAnalyzer.ResourceManagerAPIDiagnosticId, new TextSpan(708, 23), new TextSpan(686, 23)),
                    new ExpectedDiagnostic(WinUIMRTResourceManagerAnalyzer.ResourceContextAPIDiagnosticId, new TextSpan(769, 40), new TextSpan(746, 40)),
                    new ExpectedDiagnostic(WinUIMRTResourceManagerAnalyzer.ResourceContextAPIDiagnosticId, new TextSpan(849, 33), new TextSpan(825, 33))
                }
            },
            {
                "BackButtonCaller",
                new[]
                {
                    new ExpectedDiagnostic(WinUIBackButtonAnalyzer.DiagnosticId, new TextSpan(360, 78), new TextSpan(345, 78)),
                    new ExpectedDiagnostic(WinUIBackButtonAnalyzer.DiagnosticId, new TextSpan(531, 111), new TextSpan(511, 111)),
                    new ExpectedDiagnostic(WinUIBackButtonAnalyzer.DiagnosticId, new TextSpan(657, 111), new TextSpan(636, 111)),
                    new ExpectedDiagnostic(WinUIBackButtonAnalyzer.DiagnosticId, new TextSpan(928, 69), new TextSpan(901, 69)),
                    new ExpectedDiagnostic(WinUIBackButtonAnalyzer.DiagnosticId, new TextSpan(1157, 39), new TextSpan(1124, 39))
                }
            },
            {
                "AppWindowCaller",
                new[]
                {
                    new ExpectedDiagnostic(WinUIAppWindowAnalyzer.DiagnosticIdAppWindowType, new TextSpan(452, 37), new TextSpan(435, 37)),
                    new ExpectedDiagnostic(WinUIAppWindowAnalyzer.DiagnosticIdAppWindowType, new TextSpan(696, 37), new TextSpan(671, 37)),
                    new ExpectedDiagnostic(WinUIAppWindowAnalyzer.DiagnosticIdAppWindowType, new TextSpan(819, 9), new TextSpan(791, 9)),
                    new ExpectedDiagnostic(WinUIAppWindowAnalyzer.DiagnosticIdAppWindowMember, new TextSpan(935, 46), new TextSpan(902, 46)),
                    new ExpectedDiagnostic(WinUIAppWindowAnalyzer.DiagnosticIdAppWindowType, new TextSpan(1015, 15), new TextSpan(979, 15)),
                    new ExpectedDiagnostic(WinUIAppWindowAnalyzer.DiagnosticIdAppWindowType, new TextSpan(1090, 15), new TextSpan(1052, 15)),
                    new ExpectedDiagnostic(WinUIAppWindowAnalyzer.DiagnosticIdAppWindowType, new TextSpan(1122, 15), new TextSpan(1083, 15))
                }
            }
        };

        [InlineData("ContentDialogCaller")]
        [Theory]
        public async void ContentDialogCodeAnalyzerTest(string documentPath)
        {
            using var workspace = new AdhocWorkspace();
            var diagnostics = await workspace.GetDiagnosticsAsync(documentPath, ImmutableList.Create(WinUIContentDialogAnalyzer.DiagnosticId), true).ConfigureAwait(false);
            AssertDiagnosticsCorrect(diagnostics, ExpectedDiagnostics[documentPath]);
        }

        [InlineData("ContentDialogCaller")]
        [Theory]
        public async void ContentDialogCodeFixerTest(string documentPath)
        {
            using var workspace = new AdhocWorkspace();
            var actualFix = await workspace.FixSourceAsync(Language.CSharp, documentPath, ImmutableList.Create(WinUIContentDialogAnalyzer.DiagnosticId)).ConfigureAwait(false);
            var expectedFix = TestHelper.GetSource($"{documentPath}.Fixed");
            Assert.Equal(expectedFix.Trim(), actualFix.Trim());
        }

        [InlineData("DataTransferManagerCaller")]
        [Theory]
        public async void DataTransferManagertAnalyzerTest(string documentPath)
        {
            using var workspace = new AdhocWorkspace();
            var diagnostics = await workspace.GetDiagnosticsAsync(documentPath, ImmutableList.Create(WinUIDataTransferManagerAnalyzer.DiagnosticId), true).ConfigureAwait(false);
            AssertDiagnosticsCorrect(diagnostics, ExpectedDiagnostics[documentPath]);
        }

        [InlineData("DataTransferManagerCaller")]
        [Theory]
        public async void DataTransferManagerCodeFixerTest(string documentPath)
        {
            using var workspace = new AdhocWorkspace();
            var actualFix = await workspace.FixSourceAsync(Language.CSharp, documentPath, ImmutableList.Create(WinUIDataTransferManagerAnalyzer.DiagnosticId)).ConfigureAwait(false);
            var expectedFix = TestHelper.GetSource($"{documentPath}.Fixed");
            Assert.Equal(expectedFix.Trim(), actualFix.Trim());
        }

        [InlineData("InitializeWithWindow")]
        [Theory]
        public async void InitializeWithWindowTestAnalyzerTest(string documentPath)
        {
            using var workspace = new AdhocWorkspace();
            var diagnostics = await workspace.GetDiagnosticsAsync(documentPath, ImmutableList.Create(WinUIInitializeWindowAnalyzer.DiagnosticId), true).ConfigureAwait(false);
            AssertDiagnosticsCorrect(diagnostics, ExpectedDiagnostics[documentPath]);
        }

        [InlineData("InitializeWithWindow")]
        [Theory]
        public async void InitializeWithWindowTestCodeFixerTest(string documentPath)
        {
            using var workspace = new AdhocWorkspace();
            var actualFix = await workspace.FixSourceAsync(Language.CSharp, documentPath, ImmutableList.Create(WinUIInitializeWindowAnalyzer.DiagnosticId)).ConfigureAwait(false);
            var expectedFix = TestHelper.GetSource($"{documentPath}.Fixed");
            Assert.Equal(expectedFix.Trim(), actualFix.Trim());
        }

        [InlineData("InteropsCaller")]
        [Theory]
        public async void InteropsAnalyzerTest(string documentPath)
        {
            using var workspace = new AdhocWorkspace();
            var diagnostics = await workspace.GetDiagnosticsAsync(documentPath, ImmutableList.Create(WinUIInteropAnalyzer.DiagnosticId), true).ConfigureAwait(false);
            AssertDiagnosticsCorrect(diagnostics, ExpectedDiagnostics[documentPath]);
        }

        [InlineData("InteropsCaller")]
        [Theory]
        public async void InteropsCodeFixerTest(string documentPath)
        {
            using var workspace = new AdhocWorkspace();
            var actualFix = await workspace.FixSourceAsync(Language.CSharp, documentPath, ImmutableList.Create(WinUIInteropAnalyzer.DiagnosticId)).ConfigureAwait(false);
            var expectedFix = TestHelper.GetSource($"{documentPath}.Fixed");
            Assert.Equal(expectedFix.Trim(), actualFix.Trim());
        }

        [InlineData("MRTResourceManagerCaller")]
        [Theory]
        public async void ResourceManagerAnalyzerTest(string documentPath)
        {
            using var workspace = new AdhocWorkspace();
            var diagnostics = await workspace.GetDiagnosticsAsync(documentPath, ImmutableList.Create(WinUIMRTResourceManagerAnalyzer.ResourceManagerAPIDiagnosticId,
                WinUIMRTResourceManagerAnalyzer.ResourceContextAPIDiagnosticId), true).ConfigureAwait(false);
            AssertDiagnosticsCorrect(diagnostics, ExpectedDiagnostics[documentPath]);
        }

        [InlineData("MRTResourceManagerCaller")]
        [Theory]
        public async void ResourceManagerCodeFixerTest(string documentPath)
        {
            using var workspace = new AdhocWorkspace();
            var actualFix = await workspace.FixSourceAsync(Language.CSharp, documentPath, ImmutableList.Create(WinUIMRTResourceManagerAnalyzer.ResourceManagerAPIDiagnosticId,
                WinUIMRTResourceManagerAnalyzer.ResourceContextAPIDiagnosticId)).ConfigureAwait(false);
            var expectedFix = TestHelper.GetSource($"{documentPath}.Fixed");
            Assert.Equal(expectedFix.Trim(), actualFix.Trim());
        }

        [InlineData("BackButtonCaller")]
        [Theory]
        public async void BackButtonAnalyzerTest(string documentPath)
        {
            using var workspace = new AdhocWorkspace();
            var diagnostics = await workspace.GetDiagnosticsAsync(documentPath, ImmutableList.Create(WinUIBackButtonAnalyzer.DiagnosticId), true).ConfigureAwait(false);
            AssertDiagnosticsCorrect(diagnostics, ExpectedDiagnostics[documentPath]);
        }

        [InlineData("BackButtonCaller")]
        [Theory]
        public async void BackButtonCodeFixerTest(string documentPath)
        {
            using var workspace = new AdhocWorkspace();
            var actualFix = await workspace.FixSourceAsync(Language.CSharp, documentPath, ImmutableList.Create(WinUIBackButtonAnalyzer.DiagnosticId)).ConfigureAwait(false);
            var expectedFix = TestHelper.GetSource($"{documentPath}.Fixed");
            Assert.Equal(expectedFix.Trim(), actualFix.Trim());
        }

        [InlineData("AppWindowCaller")]
        [Theory]
        public async void AppWindowAnalyzerTest(string documentPath)
        {
            using var workspace = new AdhocWorkspace();
            var diagnostics = await workspace.GetDiagnosticsAsync(documentPath, ImmutableList.Create(WinUIAppWindowAnalyzer.DiagnosticIdAppWindowType,
                WinUIAppWindowAnalyzer.DiagnosticIdAppWindowVarType, WinUIAppWindowAnalyzer.DiagnosticIdAppWindowMember), true).ConfigureAwait(false);
            AssertDiagnosticsCorrect(diagnostics, ExpectedDiagnostics[documentPath]);
        }

        [InlineData("AppWindowCaller")]
        [Theory]
        public async void AppWindowCodeFixerTest(string documentPath)
        {
            using var workspace = new AdhocWorkspace();
            var actualFix = await workspace.FixSourceAsync(Language.CSharp, documentPath, ImmutableList.Create(WinUIAppWindowAnalyzer.DiagnosticIdAppWindowType,
                WinUIAppWindowAnalyzer.DiagnosticIdAppWindowVarType, WinUIAppWindowAnalyzer.DiagnosticIdAppWindowMember)).ConfigureAwait(false);
            var expectedFix = TestHelper.GetSource($"{documentPath}.Fixed");
            Assert.Equal(expectedFix.Trim(), actualFix.Trim());
        }

        private static void AssertDiagnosticsCorrect(IEnumerable<Diagnostic> diagnostics, IEnumerable<ExpectedDiagnostic> expectedDiagnostics)
        {
            Assert.Equal(expectedDiagnostics.Count(), diagnostics.Count());

            var sb = new System.Text.StringBuilder();
            var identical = true;

            var count = 0;
            foreach (var d in diagnostics.OrderBy(d => d.Location.SourceSpan.Start))
            {
                var e = expectedDiagnostics.ElementAt(count);
                var expected = $"{e.SourceSpan}";
                var actual = $"{d.Location.SourceSpan}";
                //Assert.True(e.Matches(d), $"Expected {e.Language} diagnostic {count} to be at {e.SourceSpan}" +
                //    $" ; actually at {d.Location.SourceSpan} {d.Location.SourceSpan.End - d.Location.SourceSpan.Start}");
                identical = identical && e.Matches(d);
                sb.AppendLine($"new ExpectedDiagnostic(\"{e.Id}\", new TextSpan({e.SourceSpan.Start}, {e.SourceSpan.Length}), new TextSpan({d.Location.SourceSpan.Start}, {d.Location.SourceSpan.Length})),");
                count++;
            }

            if (!identical)
            {
                var text = sb.ToString();
            }

            Assert.True(identical);
        }
    }
}
