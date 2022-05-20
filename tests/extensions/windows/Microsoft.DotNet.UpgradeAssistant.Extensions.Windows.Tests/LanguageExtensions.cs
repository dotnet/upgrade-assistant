// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.Tests
{
    public static class LanguageExtensions
    {
        public static string GetFileExtension(this Language lang) => lang switch
        {
            Language.CSharp => "cs",
            Language.VisualBasic => "vb",
            _ => throw new NotImplementedException()
        };

        public static string ToLanguageName(this Language lang) => lang switch
        {
            Language.CSharp => LanguageNames.CSharp,
            Language.FSharp => LanguageNames.FSharp,
            Language.VisualBasic => LanguageNames.VisualBasic,
            _ => throw new NotImplementedException(),
        };
    }
}
