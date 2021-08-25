// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal class ExtensionAssemblyLoadContext : AssemblyLoadContext
    {
        private const string ALC_Prefix = "UA_";

        private readonly ExtensionInstance _extension;
        private readonly ILogger _logger;

        public ExtensionAssemblyLoadContext(string instanceKey, ExtensionInstance extension, string[] assemblies, ILogger logger)
            : base(string.Concat(ALC_Prefix, extension.Name, instanceKey), isCollectible: true)
        {
            if (assemblies is null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            _extension = extension ?? throw new ArgumentNullException(nameof(extension));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Unloading += _ => _logger.LogDebug("{Name} extension is unloading", Name);

            Load(extension, assemblies);
        }

        private void Load(ExtensionInstance extension, string[] assemblies)
        {
            foreach (var path in assemblies)
            {
                try
                {
                    var fileInfo = extension.FileProvider.GetFileInfo(path);

                    if (!fileInfo.Exists)
                    {
                        _logger.LogError(" Could not find extension service provider assembly {Path} in extension {Name}", path, extension.Name);
                        continue;
                    }

                    using var assemblyStream = GetSeekableStream(fileInfo);

                    LoadFromStream(assemblyStream);
                }
                catch (FileLoadException)
                {
                }
                catch (BadImageFormatException)
                {
                }
            }
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // If available in the default, we want to ensure that is used.
            var inDefault = Default.Assemblies
                .Where(a => !a.GetName().Name!.Contains("NuGet", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.Ordinal));

            if (inDefault is Assembly existing)
            {
                _logger.LogDebug("Loaded {Name} from default context instead of {Extension}", assemblyName, Name);
                return existing;
            }

            var dll = $"{assemblyName.Name}.dll";
            var dllFile = _extension.FileProvider.GetFileInfo(dll);

            if (dllFile.Exists)
            {
                using var dllStream = GetSeekableStream(dllFile);

                var pdb = $"{assemblyName.Name}.pdb";
                var pdbFile = _extension.FileProvider.GetFileInfo(pdb);

                try
                {
                    if (pdbFile.Exists)
                    {
                        _logger.LogDebug("Loading {Name} with pdb from {Extension}", assemblyName, Name);

                        using var pdbStream = GetSeekableStream(pdbFile);
                        return LoadFromStream(dllStream, pdbStream);
                    }
                    else
                    {
                        _logger.LogDebug("Loading {Name} without pdb from {Extension}", assemblyName, Name);
                        return LoadFromStream(dllStream);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not load {Name} in {Extension}", assemblyName, Name);
                    throw;
                }
            }

            return null;
        }

        private static Stream GetSeekableStream(IFileInfo file)
        {
            var assemblyStream = file.CreateReadStream();

            if (assemblyStream.CanSeek)
            {
                return assemblyStream;
            }

            var ms = new MemoryStream();
            assemblyStream.CopyTo(ms);
            ms.Position = 0;
            assemblyStream.Dispose();
            return ms;
        }
    }
}
