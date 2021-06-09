// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    [DebuggerDisplay("{Name}, {Location}")]
    public sealed class ExtensionInstance : IDisposable
    {
        private const string ExtensionServiceProvidersSectionName = "ExtensionServiceProviders";
        public const string ManifestFileName = "ExtensionManifest.json";

        private const string ExtensionNamePropertyName = "ExtensionName";
        private const string DefaultExtensionName = "Unknown";

        private readonly Lazy<AssemblyLoadContext>? _alc;

        public ExtensionInstance(IFileProvider fileProvider, string location)
        {
            FileProvider = fileProvider;
            Location = location;
            Configuration = CreateConfiguration(fileProvider);
            Name = GetName(Configuration, location);

            var serviceProviders = GetOptions<string[]>(ExtensionServiceProvidersSectionName);

            if (serviceProviders is not null)
            {
                _alc = new Lazy<AssemblyLoadContext>(() => new ExtensionAssemblyLoadContext(this, serviceProviders));
            }
        }

        public string Name { get; }

        public IEnumerable<IExtensionServiceProvider> GetServiceProviders()
        {
            if (_alc is null)
            {
                return Enumerable.Empty<IExtensionServiceProvider>();
            }

            return _alc.Value.Assemblies.SelectMany(assembly => assembly
                .GetTypes()
                .Where(t => t.IsPublic && !t.IsAbstract && typeof(IExtensionServiceProvider).IsAssignableFrom(t))
                .Select(t => Activator.CreateInstance(t))
                .Cast<IExtensionServiceProvider>());
        }

        public string Location { get; }

        public IFileProvider FileProvider { get; }

        public IConfiguration Configuration { get; }

        public T? GetOptions<T>(string sectionName) => Configuration.GetSection(sectionName).Get<T>();

        public void Dispose()
        {
            if (FileProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Calling method will handle disposal.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Creating extensions should not throw any extensions.")]
        public static ExtensionInstance? Create(string e)
        {
            try
            {
                if (Directory.Exists(e))
                {
                    return new ExtensionInstance(new PhysicalFileProvider(e), e);
                }
                else if (File.Exists(e))
                {
                    if (ManifestFileName.Equals(Path.GetFileName(e), StringComparison.OrdinalIgnoreCase))
                    {
                        var dir = Path.GetDirectoryName(e) ?? string.Empty;
                        return new ExtensionInstance(new PhysicalFileProvider(dir), dir);
                    }
                    else if (Path.GetExtension(e).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        var provider = new ZipFileProvider(e);

                        try
                        {
                            return new ExtensionInstance(provider, e);
                        }

                        // If the manifest file couldn't be found, let's try looking at one layer deep with the name
                        // of the file as the first folder. This is what happens when you create a zip file from a folder
                        // with Windows or 7-zip
                        catch (UpgradeException ex) when (ex.InnerException is FileNotFoundException)
                        {
                            var subpath = Path.GetFileNameWithoutExtension(e);
                            var subprovider = new SubFileProvider(provider, subpath);
                            return new ExtensionInstance(subprovider, e);
                        }
                    }
                }
                else
                {
                    Console.Error.WriteLine($"ERROR: Extension {e} not found; ignoring extension {e}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: Could not load extension from {e}: {ex.Message}");
            }

            return null;
        }

        private static string GetName(IConfiguration configuration, string location)
        {
            if (configuration[ExtensionNamePropertyName] is string name)
            {
                return name;
            }

            if (Path.GetFileNameWithoutExtension(location) is string locationName)
            {
                return locationName;
            }

            return DefaultExtensionName;
        }

        private static IConfiguration CreateConfiguration(IFileProvider fileProvider)
        {
            try
            {
                return new ConfigurationBuilder()
                    .AddJsonFile(fileProvider, ManifestFileName, optional: false, reloadOnChange: false)
                    .Build();
            }
            catch (FileNotFoundException e)
            {
                throw new UpgradeException("Could not find manifest file", e);
            }
        }
    }
}
