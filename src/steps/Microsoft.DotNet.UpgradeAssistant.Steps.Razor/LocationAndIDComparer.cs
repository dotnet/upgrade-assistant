﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    public class LocationAndIDComparer : IEqualityComparer<Diagnostic>
    {
        private readonly bool _useMappedLocations;

        public LocationAndIDComparer(bool useMappedLocations)
        {
            _useMappedLocations = useMappedLocations;
        }

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
                && (_useMappedLocations
                    ? x.Location.GetMappedLineSpan().Equals(y.Location.GetMappedLineSpan())
                    : x.Location.Equals(y.Location));

            return ret;
        }

        public int GetHashCode(Diagnostic obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var hashcode = default(HashCode);
            hashcode.Add(obj.Id, StringComparer.Ordinal);
            if (_useMappedLocations)
            {
                hashcode.Add(obj.Location.GetMappedLineSpan());
            }
            else
            {
                hashcode.Add(obj.Location);
            }

            return hashcode.ToHashCode();
        }
    }
}
