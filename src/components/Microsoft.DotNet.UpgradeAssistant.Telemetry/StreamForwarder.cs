// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    internal sealed class StreamForwarder : IDisposable
    {
        private const char FlushBuilderCharacter = '\n';

        private static readonly char[] IgnoreCharacters = new char[] { '\r' };

        private StringBuilder? _builder;
        private StringWriter? _capture;
        private Action<string>? _writeLine;

        public string? CapturedOutput
        {
            get
            {
                return _capture?.GetStringBuilder()?.ToString();
            }
        }

        public StreamForwarder Capture()
        {
            ThrowIfCaptureSet();

            _capture = new StringWriter();

            return this;
        }

        public StreamForwarder ForwardTo(Action<string> writeLine)
        {
            ThrowIfForwarderSet();

            _writeLine = writeLine ?? throw new ArgumentNullException(nameof(writeLine));

            return this;
        }

        public Task BeginRead(TextReader reader)
        {
            return Task.Run(() => Read(reader));
        }

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "CA1062 doesn't understand `is not null`.")]
        public void Read(TextReader reader)
        {
            var bufferSize = 1;

            char currentCharacter;

            var buffer = new char[bufferSize];
            _builder = new StringBuilder();

            if (reader is not null)
            {
                // Using Read with buffer size 1 to prevent looping endlessly
                // like we would when using Read() with no buffer
                while (reader.Read(buffer, 0, bufferSize) > 0)
                {
                    currentCharacter = buffer[0];

                    if (currentCharacter == FlushBuilderCharacter)
                    {
                        WriteBuilder();
                    }
                    else if (!IgnoreCharacters.Contains(currentCharacter))
                    {
                        _builder.Append(currentCharacter);
                    }
                }
            }

            // Flush anything else when the stream is closed
            // Which should only happen if someone used console.Write
            WriteBuilder();

            void WriteBuilder()
            {
                if (_builder.Length == 0)
                {
                    return;
                }

                WriteLine(_builder.ToString());
                _builder.Clear();
            }
        }

        private void WriteLine(string str)
        {
            _capture?.WriteLine(str);
            _writeLine?.Invoke(str);
        }

        private void ThrowIfForwarderSet()
        {
            if (_writeLine != null)
            {
                throw new InvalidOperationException("WriteLine forwarder set previously"); // TODO: Localize this?
            }
        }

        private void ThrowIfCaptureSet()
        {
            if (_capture != null)
            {
                throw new InvalidOperationException("Already capturing stream!"); // TODO: Localize this?
            }
        }

        public void Dispose()
        {
            _capture?.Dispose();
            _capture = null;
        }
    }
}
