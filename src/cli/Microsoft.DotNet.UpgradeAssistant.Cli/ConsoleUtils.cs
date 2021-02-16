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
