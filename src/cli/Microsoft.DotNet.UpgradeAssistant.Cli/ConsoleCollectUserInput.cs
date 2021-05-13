// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class ConsoleCollectUserInput : IUserInput
    {
        private const string Prompt = "> ";
        private readonly InputOutputStreams _io;
        private readonly ILogger<ConsoleCollectUserInput> _logger;

        public ConsoleCollectUserInput(InputOutputStreams io, ILogger<ConsoleCollectUserInput> logger)
        {
            _io = io ?? throw new ArgumentNullException(nameof(io));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsInteractive => true;

        public Task<string?> AskUserAsync(string prompt)
        {
            _io.Output.WriteLine(prompt);
            _io.Output.Write(Prompt);

            return _io.Input.ReadLineAsync();
        }

        public async Task<T> ChooseAsync<T>(string message, IEnumerable<T> commands, CancellationToken token)
            where T : UpgradeCommand
        {
            if (commands is null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            await _io.Output.WriteLineAsync(message);

            var possible = new Dictionary<int, T>();

            foreach (var command in commands)
            {
                if (command.IsEnabled)
                {
                    var index = possible.Count + 1;
                    await _io.Output.WriteLineAsync($" {index,3}. {command.CommandText}");
                    possible.Add(index, command);
                }
                else
                {
                    await _io.Output.WriteLineAsync($"   {command.CommandText}");
                }
            }

            while (true)
            {
                token.ThrowIfCancellationRequested();

                await _io.Output.WriteAsync(Prompt);

                var result = await _io.Input.ReadLineAsync();

                if (result is null)
                {
                    throw new OperationCanceledException();
                }

                var selectedCommandText = result.Trim(' ', '.', '\t');

                if (selectedCommandText is { Length: 0 })
                {
                    if (possible.TryGetValue(1, out var defaultSelected))
                    {
                        return defaultSelected;
                    }
                }
                else if (int.TryParse(selectedCommandText, out var selectedCommandIndex))
                {
                    if (possible.TryGetValue(selectedCommandIndex, out var selected))
                    {
                        return selected;
                    }
                }

                _logger.LogError("Unknown selection: '{Index}'", selectedCommandText.ToString());
            }
        }

        public Task<bool> WaitToProceedAsync(CancellationToken token)
        {
            Console.WriteLine("Please press enter to continue...");

            return Console.ReadLine() is not null
                ? Task.FromResult(true)
                : Task.FromResult(false);
        }
    }
}
