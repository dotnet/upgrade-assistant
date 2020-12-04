using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.ConsoleApp
{
    public class ConsoleCollectUserInput : ICollectUserInput
    {
        private const string Prompt = "> ";

        private readonly ILogger<ConsoleCollectUserInput> _logger;

        public ConsoleCollectUserInput(ILogger<ConsoleCollectUserInput> logger)
        {
            _logger = logger;
        }

        public Task<string?> AskUserAsync(string prompt)
        {
            Console.WriteLine(prompt);
            Console.Write(Prompt);

            return Task.FromResult(Console.ReadLine());
        }

        public Task<T> ChooseAsync<T>(string message, IEnumerable<T> commands, CancellationToken token)
            where T : MigrationCommand
        {
            if (commands is null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            var listOfCommands = commands as IReadOnlyList<T> ?? new List<T>(commands);

            Console.WriteLine(message);

            for (var i = 0; i < listOfCommands.Count; i++)
            {
                Console.WriteLine($" {i + 1}. {listOfCommands[i].CommandText}");
            }

            while (true)
            {
                token.ThrowIfCancellationRequested();

                Console.Write(Prompt);

                var result = Console.ReadLine();

                if (result is null)
                {
                    throw new OperationCanceledException();
                }

                var selectedCommandText = result.AsSpan().Trim(" .\t");

                if (int.TryParse(selectedCommandText, out var selectedCommandIndex))
                {
                    selectedCommandIndex--;
                    if (selectedCommandIndex >= 0 && selectedCommandIndex < listOfCommands.Count)
                    {
                        return Task.FromResult(listOfCommands[selectedCommandIndex]);
                    }
                }

                _logger.LogError("Unknown selection: '{Index}'", selectedCommandText.ToString());
            }
        }
    }
}
