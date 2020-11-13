using System;
using System.Text;
using System.Threading.Tasks;
using AspNetMigrator.Engine.GlobalCommands;

namespace AspNetMigrator.ConsoleApp
{
    public abstract class CommandResultHandler
    {
        public abstract void HandleResult(bool commandResult);
    }

    public class ApplyNextCommandResultHandler : CommandResultHandler
    {
        public override void HandleResult(bool commandResult)
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

    public class ConfigureLoggingCommandResultHandler : CommandResultHandler
    {
        public override void HandleResult(bool commandResult)
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Logging configuration not yet implemented.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
        }
    }

    public class SeeMoreDetailsCommandResultHandler: CommandResultHandler
    {
        private const int DefaultWidth = 80;

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

        public override void HandleResult(bool commandResult)
        {

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

    public class SkipNextCommandResultHandler : CommandResultHandler
    {
        public override void HandleResult(bool commandResult)
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

    public class UnknownCommandResultHandler : CommandResultHandler
    {
        public override void HandleResult(bool commandResult)
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Unknown command");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
        }
    }

    public class SetBackupPathCommandResultHandler : CommandResultHandler
    {
        public override void HandleResult(bool commandResult)
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Not yet implemented");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
        }
    }
}
