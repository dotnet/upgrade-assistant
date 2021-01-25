using System.Collections.Generic;
using System.IO;

namespace AspNetMigrator.Extensions
{
    public interface IExtensionProvider
    {
        string Name { get; }

        string? GetSetting(string settingName);

        Stream? GetFile(string path);

        IEnumerable<string> ListFiles(string path, string searchPattern);

        IEnumerable<string> ListFiles(string path);
    }
}
