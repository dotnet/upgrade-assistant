// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public record AdapterDescriptorFactory(string Namespace, string Original, string Destination)
    {
        public string FullDestination => $"{Namespace}.{Destination}";

        public string FullOriginal => $"{Namespace}.{Original}";

        public string CreateAttributeString(string languageName)
            => languageName switch
            {
                LanguageNames.VisualBasic => CreateVBAttributeString(),
                LanguageNames.CSharp => CreateCSharpAttributeString(),
                _ => throw new NotSupportedException(),
            };

        private string CreateVBAttributeString() => Destination is null
            ? $"<Assembly: Microsoft.CodeAnalysis.Refactoring.AdapterDescriptor(GetType({FullOriginal}))>"
            : $"<Assembly: Microsoft.CodeAnalysis.Refactoring.AdapterDescriptor(GetType({FullOriginal}), GetType({FullDestination}))>";

        private string CreateCSharpAttributeString() => Destination is null
            ? $"[assembly: global::Microsoft.CodeAnalysis.Refactoring.AdapterDescriptor(typeof({FullOriginal}))]"
            : $"[assembly: global::Microsoft.CodeAnalysis.Refactoring.AdapterDescriptor(typeof({FullOriginal}), typeof({FullDestination}))]";
    }
}
