using System.Collections.Generic;
using System.IO;

namespace AspNetMigrator.Extensions
{
    public interface IExtensionProvider
    {
        string Name { get; }

        T? GetOptions<T>(string sectionName);

        Stream? GetFile(string path);

        IEnumerable<string> ListFiles(string path, string searchPattern);

        IEnumerable<string> ListFiles(string path);
    }
}
