// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.DotNet.UpgradeAssistant.Analysis;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IOutputResultWriterProvider
    {
        bool TryGetWriter(string format, [MaybeNullWhen(false)] out IOutputResultWriter writer);
    }

    public class AnalyzerResultProviderWriter : IOutputResultWriterProvider
    {
    private readonly IEnumerable<IOutputResultWriter> _writers;

    public AnalyzerResultProviderWriter(IEnumerable<IOutputResultWriter> writers)
    {
        _writers = writers;
    }

    public bool TryGetWriter(string format, [MaybeNullWhen(false)] out IOutputResultWriter writer)
    {
            foreach (var writ in _writers)
            {
                if (string.Equals(writ.Format, format, StringComparison.OrdinalIgnoreCase))
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
