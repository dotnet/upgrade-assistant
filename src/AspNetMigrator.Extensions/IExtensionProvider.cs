using System.Collections.Generic;
using System.IO;

namespace AspNetMigrator.Extensions
{
    public interface IExtensionProvider
    {
        string? GetSetting(string settingName);

        Stream? GetFile(string path);

        IEnumerable<string> ListFiles(string path);
    }
}
