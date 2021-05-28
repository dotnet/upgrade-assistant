// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal sealed class ExtensionInstance : IDisposable
    {
        private const string ManifestFileName = "ExtensionManifest.json";
        private const string ExtensionNamePropertyName = "ExtensionName";
        private const string DefaultExtensionName = "Unknown";

        private readonly Lazy<AssemblyLoadContext> _alc;

        public ExtensionInstance(IFileProvider fileProvider, string? name = null, IConfiguration? configuration = null)
        {
            FileProvider = fileProvider;
            Configuration = configuration ?? CreateConfiguration(fileProvider);
            Name = name ?? GetName(Configuration, FileProvider);
            _alc = new Lazy<AssemblyLoadContext>(() => new ExtensionAssemblyLoadContext(this));
        }

        public string Name { get; }

        public bool HasAssemblyLoadContext => _alc.IsValueCreated;

        /// <summary>
        /// Gets the <see cref="AssemblyLoadContext"/> for the extension. Guard calls with <see cref="HasAssemblyLoadContext"/> first,
        /// otherwise it may trigger creation of the load context if it is not needed.
        /// </summary>
        public AssemblyLoadContext LoadContext => _alc.Value;

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
                    return new ExtensionInstance(new PhysicalFileProvider(e));
                }
                else if (File.Exists(e))
                {
                    if (ManifestFileName.Equals(Path.GetFileName(e), StringComparison.OrdinalIgnoreCase))
                    {
                        var dir = Path.GetDirectoryName(e) ?? string.Empty;
                        return new ExtensionInstance(new PhysicalFileProvider(dir));
                    }
                    else if (Path.GetExtension(e).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        var provider = new ZipFileProvider(e);

                        return new ExtensionInstance(new ZipFileProvider(e));
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

        private static string GetName(IConfiguration configuration, IFileProvider fileProvider)
        {
            if (configuration[ExtensionNamePropertyName] is string name)
            {
                return name;
            }

            if (fileProvider is PhysicalFileProvider physical)
            {
                return physical.Root;
            }

            return DefaultExtensionName;
        }

        private static IConfiguration CreateConfiguration(IFileProvider fileProvider)
            => new ConfigurationBuilder()
                .AddJsonFile(fileProvider, ManifestFileName, optional: false, reloadOnChange: false)
                .Build();
    }
}
