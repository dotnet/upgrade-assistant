// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.DotNet.UpgradeAssistant.Analysis;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IAnalyzeResultWriterProvider
    {
        bool TryGetWriter(string format,[MaybeNullWhen(false)] out IAnalyzeResultWriter writer);
    }

    public class AnalyzerResultProviderWriter : IAnalyzeResultWriterProvider
    {

    private readonly IEnumerable<IAnalyzeResultWriter> _writers;

    public AnalyzerResultProviderWriter(IEnumerable<IAnalyzeResultWriter> writers)
    {
        _writers = writers;
    }

    public bool TryGetWriter(string format, [MaybeNullWhen(false)] out IAnalyzeResultWriter writer)
    {

            foreach (var writ in _writers)
            {
                if (string.Equals(writ.GetFormat(), format, StringComparison.OrdinalIgnoreCase))
                {
                    writer = writ;
                    return true;
                }
            }

            writer = null;
            return false;
        }
    }
}
