using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IExtensionProvider
    {
        string Name { get; }

        T? GetOptions<T>(string sectionName);

        Stream? GetFile(string path);

        IEnumerable<string> GetFiles(string path, string searchPattern);

        IEnumerable<string> GetFiles(string path);
    }
}
