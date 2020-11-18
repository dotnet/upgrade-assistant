using System;
using System.Text;
using System.Threading.Tasks;
using AspNetMigrator.Engine;

namespace AspNetMigrator.ConsoleApp
{
    public static class ConsoleHelpers
    {
        private const int DefaultWidth = 80;

        public static Task SendMessageToUserAsync(UserMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            switch (message.Severity)
            {
                case MessageSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.Cyan; break;
                case MessageSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow; break;
                case MessageSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red; break;
                default:
                    Console.ResetColor(); break;
            }

            Console.WriteLine(WrapString(message?.Message, Console.WindowWidth));
            Console.ResetColor();

            return Task.CompletedTask;
        }

        public static string WrapString(string input, int lineLength = DefaultWidth)
        {
            if (input is null)
            {
                return null;
            }

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
}
