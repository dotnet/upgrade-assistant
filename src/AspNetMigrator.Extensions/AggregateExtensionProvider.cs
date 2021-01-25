using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace AspNetMigrator.Extensions
{
    public class AggregateExtensionProvider : IExtensionProvider
    {
        /// <summary>
        /// Gets underlying extension providers.
        /// </summary>
        public ImmutableArray<IExtensionProvider> ExtensionProviders { get; }

        public AggregateExtensionProvider(IEnumerable<IExtensionProvider> extensionProviders)
        {
            if (extensionProviders is null)
            {
                 throw new ArgumentNullException(nameof(extensionProviders));
            }

            ExtensionProviders = ImmutableArray.CreateRange(extensionProviders);
        }

        /// <summary>
        /// Gets a file from the specified path from underlying extension providers.
        /// </summary>
        /// <param name="path">The file to open.</param>
        /// <returns>Stream contents of the specified file. If the files is provided by more than one extension provider, returns the files from the provider registered last. If no provider provides the files, returns null.</returns>
        public Stream? GetFile(string path)
        {
            Stream? ret = null;

            foreach (var file in ExtensionProviders.Select(p => p.GetFile(path)))
            {
                if (file is not null)
                {
                    // Make sure to dispose any streams that aren't returned
                    ret?.Dispose();
                    ret = file;
                }
            }

            return ret;
        }

        /// <summary>
        /// Gets a setting value from underlying extension providers.
        /// </summary>
        /// <param name="settingName">The setting to get a value for.</param>
        /// <returns>The value of the specified setting. If the setting is provided by more than one extension provider, returns the setting from the provider registered last. If no provider provides the setting, returns null.</returns>
        public string? GetSetting(string settingName) =>
            ExtensionProviders.Select(p => p.GetSetting(settingName)).Where(s => s is not null).LastOrDefault();

        /// <summary>
        /// Returns a list of files present at the specified path from any underlying extension providers.
        /// </summary>
        /// <param name="path">The path to look in for files.</param>
        /// <returns>All files in or under the given path from any extension provider.</returns>
        public IEnumerable<string> ListFiles(string path) =>
            ExtensionProviders.SelectMany(p => p.ListFiles(path)).Distinct();
    }
}
