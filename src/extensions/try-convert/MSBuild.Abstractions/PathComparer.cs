// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace MSBuild.Abstractions
{
    public class PathComparer : IEqualityComparer<string>
    {
        public static readonly PathComparer Default = new PathComparer();

        public PathComparer()
        {
        }

        public bool Equals([AllowNull] string x, [AllowNull] string y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null)
            {
                return false;
            }

            if (y == null)
            {
                return false;
            }

            var xPath = PathHelpers.GetIncludePath(x);
            var yPath = PathHelpers.GetIncludePath(y);

            return xPath.Equals(yPath, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            var path = PathHelpers.GetIncludePath(obj ?? string.Empty);

            return path.GetHashCode();
        }
    }
}
