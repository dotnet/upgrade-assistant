// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Common
{
    public static class SymbolExtensions
    {
        public static bool NameEquals(this IAssemblySymbol? symbol, string name, bool startsWith = true)
        {
            if (symbol is null)
            {
                return false;
            }

            if (startsWith)
            {
                return symbol.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return string.Equals(symbol.Name, name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
