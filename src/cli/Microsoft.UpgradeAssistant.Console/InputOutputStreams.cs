using System.IO;

namespace Microsoft.UpgradeAssistant.Cli
{
    public record InputOutputStreams(TextReader Input, TextWriter Output);
}
