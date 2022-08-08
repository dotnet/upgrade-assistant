// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace MSBuild.Abstractions
{
    public static class Extensions
    {
        public static bool ContainsIgnoreCase(this string target, string substring)
        {
            return target.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
