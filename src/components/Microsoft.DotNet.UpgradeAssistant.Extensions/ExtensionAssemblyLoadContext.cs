// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal class ExtensionAssemblyLoadContext : AssemblyLoadContext
    {
        private const string UpgradeAssistantFilePrefix = "Microsoft.DotNet.UpgradeAssistant.";
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

            // The resolving event is called if Load(AssemblyName) fails to load the assembly *and*
            // falling back to the default ALC fails to load the assembly. In such cases, we will try to load again,
            // this time permitting the extension to load Microsoft.DotNet.UpgradeAssistant.* assemblies (which
            // isn't typically permitted in Load) in order to support the unlikely case that the extension
            // ships a Microsoft.DotNet.UpgradeAssistant.* assembly that isn't part of the shared UA infrastructure.
            // Relevant loader docs: https://docs.microsoft.com/dotnet/core/dependency-loading/loading-managed#algorithm
            Resolving += (alc, name) => Load(name, true);

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

        // When Load is called, we will not load assemblies beginning with the prefix Microsoft.DotNet.UpgradeAssistant.*
        // because those assemblies should typically come from the default ALC so that they're shared. This prevents
        // an extension that accidentally has UA binaries in its binary directory from loading a different instance
        // of a UA assembly and running into issues with types not resolving.
        protected override Assembly? Load(AssemblyName assemblyName) => Load(assemblyName, false);

        /// <summary>
        /// Attempts to load an assembly given the assembly's name by probing with the extension's
        /// file provider (which typically means looking in the extension's path).
        /// If the assembly is not successfully loaded, the loader will fallback to attempting to 
        /// load the assembly from the default assembly load context.
        /// </summary>
        /// <param name="assemblyName">The assembly name to load.</param>
        /// <param name="loadUpgradeAssistantAssemblies">
        /// True if the load should consider files beginning with UA-specific prefixes
        /// (Microsoft.DotNet.UpgradeAssistant.*) or false if such assemblies should not be loaded
        /// in this assembly load context.
        /// </param>
        /// <returns>The assembly corresponding to the given assembly name or null if none was found.</returns>
        protected Assembly? Load(AssemblyName assemblyName, bool loadUpgradeAssistantAssemblies)
        {
            if (assemblyName.Name is null)
            {
                return null;
            }

            // Don't load Microsoft.DotNet.UpgradeAssistant.* assemblies unless the caller specified that
            // such assemblies should be loaded from extension-local file paths.
            if (assemblyName.Name.StartsWith(UpgradeAssistantFilePrefix, StringComparison.OrdinalIgnoreCase) && !loadUpgradeAssistantAssemblies)
            {
                return null;
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
