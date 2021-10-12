// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public record AdapterDescriptorFactory(string Namespace, string Destination, string Original)
    {
        public string FullDestination => $"{Namespace}.{Destination}";

        public string FullOriginal => $"{Namespace}.{Original}";

        public string CreateAttributeString(string languageName)
            => languageName switch
            {
                LanguageNames.VisualBasic => $"<Assembly: Microsoft.CodeAnalysis.AdapterDescriptor(GetType({FullDestination}), GetType({FullOriginal}))>",
                LanguageNames.CSharp => $"[assembly: global::Microsoft.CodeAnalysis.AdapterDescriptor(typeof({FullDestination}), typeof({FullOriginal}))]",
                _ => throw new NotSupportedException(),
            };
    }
}
