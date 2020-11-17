using System;
using System.Threading.Tasks;
using AspNetMigrator.Engine;

namespace AspNetMigrator.ConsoleApp
{
    public class CollectBackupPathFromConsole : ICollectUserInput
    {
        public Task<string> AskUser(string currentBackupPath)
        {
            // todo - support localization
            Console.Write($"Current backup path: {currentBackupPath}");
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Please specify the new path and then press {enter} to continue");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            return Task.FromResult(Console.ReadLine());
        }
    }
}
