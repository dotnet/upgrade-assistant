// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.Tests
{
    public class ExpectedDiagnostic
    {
        public string Id { get; }

        public TextSpan SourceSpan { get; }

        public Language Language { get; }

        public ExpectedDiagnostic(string id, TextSpan sourceSpan, Language lang = Language.CSharp)
        {
            Id = id;
            SourceSpan = sourceSpan;
            Language = lang;
        }

        public bool Matches(Diagnostic diagnostic) => (diagnostic?.Id.Equals(Id, StringComparison.Ordinal) ?? false) && diagnostic.Location.SourceSpan.Equals(SourceSpan);
    }
}
