using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.UpgradeAssistant.Cli
{
    public class ConsoleCollectUserInput : ICollectUserInput
    {
        private const string Prompt = "> ";
        private readonly InputOutputStreams _io;
        private readonly ILogger<ConsoleCollectUserInput> _logger;

        public ConsoleCollectUserInput(InputOutputStreams io, ILogger<ConsoleCollectUserInput> logger)
        {
            _io = io ?? throw new ArgumentNullException(nameof(io));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<string?> AskUserAsync(string prompt)
        {
            _io.Output.WriteLine(prompt);
            _io.Output.Write(Prompt);

            return Task.FromResult(_io.Input.ReadLine());
        }

        public Task<T> ChooseAsync<T>(string message, IEnumerable<T> commands, CancellationToken token)
            where T : MigrationCommand
        {
            if (commands is null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            _io.Output.WriteLine(message);

            var possible = new Dictionary<int, T>();

            foreach (var command in commands)
            {
                if (command.IsEnabled)
                {
                    var index = possible.Count + 1;
                    _io.Output.WriteLine($" {index,3}. {command.CommandText}");
                    possible.Add(index, command);
                }
                else
                {
                    _io.Output.WriteLine($"   {command.CommandText}");
                }
            }

            while (true)
            {
                token.ThrowIfCancellationRequested();

                _io.Output.Write(Prompt);

                var result = _io.Input.ReadLine();

                if (result is null)
                {
                    throw new OperationCanceledException();
                }

                var selectedCommandText = result.AsSpan().Trim(" .\t");

                if (int.TryParse(selectedCommandText, out var selectedCommandIndex))
                {
                    if (possible.TryGetValue(selectedCommandIndex, out var selected))
                    {
                        return Task.FromResult(selected);
                    }
                }

                _logger.LogError("Unknown selection: '{Index}'", selectedCommandText.ToString());
            }
        }
    }
}
