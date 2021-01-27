using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using AutoMapper;

namespace AspNetMigrator.Extensions
{
    public class AggregateExtensionProvider : IExtensionProvider
    {
        public string Name => $"Aggregate extensions from {ExtensionProviders.Length} underlying providers";

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
        /// Creates an object representing the given config section in extension manifests.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize.</typeparam>
        /// <param name="sectionName">The name of the section to read from extension manifests.</param>
        /// <returns>An object representing the specified config section. If more than one extension provider has the indicated config section, providers registered later can override some or all of the object's properties. If no provider provides the config section, returns null.</returns>
        public T? GetOptions<T>(string sectionName)
        {
            // Use Automapper here as it's more readable and more performant than
            // doing the mapping ourselves with reflection
            var mapper = GetMapper<T>();
            T? options = default;

            foreach (var extension in ExtensionProviders)
            {
                var newOptions = extension.GetOptions<T>(sectionName);

                if (newOptions is null)
                {
                    // If this extension doesn't provide the option, continue
                    continue;
                }
                else if (options is null)
                {
                    // If the option was not previously set, set it now
                    options = newOptions;
                }
                else
                {
                    // Update properties that are set in the new options
                    options = mapper.Map(newOptions, options);
                }
            }

            return options;
        }

        private static Mapper GetMapper<T>()
        {
            var mapperConfiguration = new MapperConfiguration(config =>
            {
                config.CreateMap<T, T>()
                    .ForAllMembers(options => options.Condition((src, dest, member) =>
                    {
                        // Don't overwrite older options with newer ones
                        // that are null of empty.
                        if (member is null)
                        {
                            return false;
                        }

                        if (member is Array a && a.Length == 0)
                        {
                            return false;
                        }

                        if (member is string s && s.Length == 0)
                        {
                            return false;
                        }

                        return true;
                    }));
            });
            var mapper = new Mapper(mapperConfiguration);
            return mapper;
        }

        /// <summary>
        /// Returns a list of files present at the specified path from any underlying extension providers.
        /// </summary>
        /// <param name="path">The path to look in for files.</param>
        /// <returns>All files in or under the given path from any extension provider.</returns>
        public IEnumerable<string> GetFiles(string path) =>
            ExtensionProviders.SelectMany(p => p.GetFiles(path)).Distinct();

        /// <summary>
        /// Returns a list of files present at the specified path from any underlying extension providers.
        /// </summary>
        /// <param name="path">The path to look in for files.</param>
        /// <param name="searchPattern">Search pattern that files should match (supports wildcards like * and ?).</param>
        /// <returns>All files in or under the given path from any extension provider.</returns>
        public IEnumerable<string> GetFiles(string path, string searchPattern) =>
            ExtensionProviders.SelectMany(p => p.GetFiles(path, searchPattern)).Distinct();
    }
}
