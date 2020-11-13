using System;

namespace AspNetMigrator.ConsoleApp
{
    public static class SupportedActions
    {
        public static void NotImplemented()
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Logging configuration not yet implemented.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
        }
    }
}
