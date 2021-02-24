// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    internal static class ConsoleUtils
    {
        public static void Clear()
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
            }
        }
    }
}
