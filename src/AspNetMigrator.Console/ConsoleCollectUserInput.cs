using System;
using System.Threading.Tasks;
using AspNetMigrator.Engine;

namespace AspNetMigrator.ConsoleApp
{
    public class ConsoleCollectUserInput : ICollectUserInput
    {
        public Task<string?> AskUserAsync(string prompt)
        {
            Console.WriteLine(prompt);
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.Write("> ");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            return Task.FromResult(Console.ReadLine());
        }
    }
}
