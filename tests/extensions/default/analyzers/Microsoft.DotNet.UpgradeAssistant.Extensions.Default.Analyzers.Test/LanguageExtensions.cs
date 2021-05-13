// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public static class LanguageExtensions
    {
        public static string GetFileExtension(this Language lang)
        {
            return lang switch
            {
                Language.CSharp => "cs",
                Language.VisualBasic => "vb",
                _ => throw new NotImplementedException()
            };
        }
    }
}
