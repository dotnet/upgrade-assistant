// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    public class MappedLocationAndIDComparer : IEqualityComparer<Diagnostic>
    {
        public bool Equals(Diagnostic x, Diagnostic y)
        {
            if (x is null)
            {
                return y is null;
            }

            if (y is null)
            {
                return false;
            }

            var ret = x.Id.Equals(y.Id, StringComparison.Ordinal)
                && x.Location.GetMappedLineSpan().Equals(y.Location.GetMappedLineSpan());

            return ret;
        }

        public int GetHashCode(Diagnostic diagnostic)
        {
            // https://stackoverflow.com/a/1646913
            var hash = 17;
            hash = (hash * 31) + diagnostic.Id.GetHashCode();
            hash = (hash * 31) + diagnostic.Location.GetMappedLineSpan().GetHashCode();
            return hash;
        }
    }
}
