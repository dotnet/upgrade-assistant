// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public class ExpectedDiagnostic
    {
        public string Id { get; }

        public TextSpan SourceSpan { get; }

        public Language Language { get; }

        public ExpectedDiagnostic(string id, TextSpan windowsSourceSpan, TextSpan unixSourceSpan, Language lang = Language.CSharp)
        {
            Id = id;
            Language = lang;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SourceSpan = windowsSourceSpan;
            }
            else
            {
                SourceSpan = unixSourceSpan;
            }
        }

        public bool Matches(Diagnostic diagnostic) => (diagnostic?.Id.Equals(Id, StringComparison.Ordinal) ?? false) && diagnostic.Location.SourceSpan.Equals(SourceSpan);
    }
}
