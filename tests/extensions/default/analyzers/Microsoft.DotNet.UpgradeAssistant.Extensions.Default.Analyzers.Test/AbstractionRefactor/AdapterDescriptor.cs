// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public record AdapterDescriptor(string Namespace, string Destination, string Original)
    {
        public string CreateAttributeString(string languageName)
            => languageName switch
            {
                LanguageNames.VisualBasic => $"<Assembly: Microsoft.CodeAnalysis.AdapterDescriptor(GetType({Namespace}.{Destination}), GetType({Namespace}.{Original}))>",
                LanguageNames.CSharp => $"[assembly: global::Microsoft.CodeAnalysis.AdapterDescriptor(typeof({Namespace}.{Destination}), typeof({Namespace}.{Original}))]",
                _ => throw new NotSupportedException(),
            };
    }
}
