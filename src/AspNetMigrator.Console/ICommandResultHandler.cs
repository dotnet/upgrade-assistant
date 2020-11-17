using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AspNetMigrator.Engine;
using AspNetMigrator.Engine.GlobalCommands;

namespace AspNetMigrator.ConsoleApp
{
    public interface ICommandResultHandler
    {
        Type GetTypeOfCommand();

        void HandleResult(bool commandResult);
    }

    public class ApplyNextCommandResultHandler : ICommandResultHandler
    {
        public Type GetTypeOfCommand() => typeof(ApplyNextCommand);

        public void HandleResult(bool commandResult)
        {
            if (!commandResult)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                Console.WriteLine("No migration step applied");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                Console.ResetColor();
            }
        }
    }

    public class ConfigureLoggingCommandResultHandler : ICommandResultHandler
    {
        public Type GetTypeOfCommand() => typeof(ConfigureLoggingCommand);

        public void HandleResult(bool commandResult)
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Logging configuration not yet implemented.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
        }
    }

    public class SeeMoreDetailsCommandResultHandler : ICommandResultHandler
    {
        private const int DefaultWidth = 80;

        public Type GetTypeOfCommand() => typeof(SeeMoreDetailsCommand);

        public static async Task SendAllMessagesToUserAsync(List<UserMessage> listOfMessages)
        {
            if (listOfMessages == null)
            {
                throw new ArgumentNullException(nameof(listOfMessages));
            }

            foreach (var message in listOfMessages)
            {
                await SendMessageToUserAsync(message).ConfigureAwait(false);
            }
        }

        public static Task SendMessageToUserAsync(UserMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            switch (message.Category)
            {
                case UserMessageCategory.Info:
                    Console.ForegroundColor = ConsoleColor.Cyan; break;
                case UserMessageCategory.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow; break;
                default:
                    Console.ResetColor(); break;
            }

            Console.WriteLine(WrapString(message?.Message, Console.WindowWidth));
            Console.ResetColor();

            return Task.CompletedTask;
        }

        public void HandleResult(bool commandResult)
        {
            // nothing to do, the output was sent direct from the command
        }

        private static string WrapString(string input, int lineLength = DefaultWidth)
        {
            var word = new StringBuilder();
            var ret = new StringBuilder();
            var index = 0;
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\n':
                    case '\r':
                        AddWordToRet();
                        ret.Append(c);
                        index = 0;
                        break;
                    case '\t':
                        AddWordToRet();
                        word.Append("    ");
                        AddWordToRet();
                        break;
                    case ' ':
                        word.Append(c);
                        AddWordToRet();
                        break;
                    default:
                        word.Append(c);
                        break;
                }
            }

            AddWordToRet();

            return ret.ToString();

            void AddWordToRet()
            {
                if (index + word.Length >= lineLength)
                {
                    ret.AppendLine();
                    index = 0;
                }

                ret.Append(word);
                index += word.Length;
                word = new StringBuilder();
            }
        }
    }

    public class SkipNextCommandResultHandler : ICommandResultHandler
    {
        public Type GetTypeOfCommand() => typeof(SkipNextCommand);

        public void HandleResult(bool commandResult)
        {
            if (!commandResult)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                Console.WriteLine("Skip step failed");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                Console.ResetColor();
            }
        }
    }

    public class UnknownCommandResultHandler : ICommandResultHandler
    {
        public Type GetTypeOfCommand() => typeof(UnknownCommand);

        public void HandleResult(bool commandResult)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Unknown command");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            Console.ResetColor();
        }
    }

    public class SetBackupPathCommandResultHandler : ICommandResultHandler
    {
        private readonly BackupStep _backupStep;

        public Type GetTypeOfCommand() => typeof(SetBackupPathCommand);

        public SetBackupPathCommandResultHandler(BackupStep backupStep)
        {
            _backupStep = backupStep ?? throw new ArgumentNullException(nameof(backupStep));
        }

        public void HandleResult(bool commandResult)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("The backup path is now set to: ");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            Console.ResetColor();
            Console.WriteLine(_backupStep.GetBackupPath());
            Console.WriteLine();
        }
    }

    public class ExitCommandResultHandler : ICommandResultHandler
    {
        public Type GetTypeOfCommand() => typeof(ExitCommand);

        public void HandleResult(bool commandResult)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Exiting...");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            Console.ResetColor();
        }
    }
}
