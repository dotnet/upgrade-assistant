// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public static class ConsoleHelpers
    {
        private const int DefaultWidth = 80;

        public static string WrapString(string input, int lineLength = DefaultWidth, int offset = 0)
        {
            if (input is null)
            {
                return string.Empty;
            }

            var word = new StringBuilder();
            var ret = new StringBuilder();
            var index = offset;
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\n':
                    case '\r':
                        AddWordToRet();
                        ret.Append(c);
                        index = offset;
                        ret.Append(' ', offset);
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

            return ret.ToString().TrimEnd();

            void AddWordToRet()
            {
                if (index + word.Length >= lineLength)
                {
                    ret.AppendLine();
                    index = offset;
                    ret.Append(' ', offset);
                }

                ret.Append(word);
                index += word.Length;
                word = new StringBuilder();
            }
        }
    }
}
