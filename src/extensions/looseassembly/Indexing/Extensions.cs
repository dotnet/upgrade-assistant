// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Indexing
{
    /// <summary>
    /// Useful extensions.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Reads a struct straight from a byte span.
        /// </summary>
        public static T Read<T>(this ReadOnlySpan<byte> span)
            where T : struct
        {
            return MemoryMarshal.Read<T>(span);
        }

        /// <summary>
        /// Reads a null-terminated UTF8 string from a byte span.
        /// </summary>
        /// <param name="span">The span that contains the string at the first byte.</param>
        /// <param name="consumed">The number of bytes that were "consumed" in the reading of the string.
        /// Useful for "advancing" the span after reading the string.</param>
        public static string ReadNullTerminatedUtf8String(this ReadOnlySpan<byte> span, out int consumed)
        {
            var i = 0;
            while (span[i] != 0)
            {
                i++;
            }

            span = span.Slice(0, i);
            consumed = i + 1;
            return Encoding.UTF8.GetString(span);
        }
    }
}
