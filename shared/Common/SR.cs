// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System
{
    internal static class SR
    {
        public static string Format(string str, object? arg0)
            => string.Format(CultureInfo.InvariantCulture, str, arg0);
    }
}
