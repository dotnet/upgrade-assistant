using System.IO;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public record InputOutputStreams(TextReader Input, TextWriter Output);
}
