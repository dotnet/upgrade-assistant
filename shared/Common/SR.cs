// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System
{
    internal static class SR
    {
        public static string Format(string str, object? arg0)
            => string.Format(System.Globalization.CultureInfo.InvariantCulture, str, arg0);

        public static string Format(string str, object? arg0, object? arg1)
           => string.Format(System.Globalization.CultureInfo.InvariantCulture, str, arg0, arg1);

        public static string Format(string str, object? arg0, object? arg1, object? arg2)
            => string.Format(System.Globalization.CultureInfo.InvariantCulture, str, arg0, arg1, arg2);
    }
}
