// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal class ExtensionAssemblyLoadContext : AssemblyLoadContext
    {
        private const string ALC_Prefix = "UA_";

        private readonly ExtensionInstance _extension;

        public ExtensionAssemblyLoadContext(ExtensionInstance extension, string[] assemblies)
            : base(ALC_Prefix + extension.Name)
        {
            _extension = extension;
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
                        Console.WriteLine($"ERROR: Could not find extension service provider assembly {path} in extension {extension.Name}");
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
            var inDefault = Default.Assemblies.FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.Ordinal));

            if (inDefault is Assembly existing)
            {
                return existing;
            }

            var dll = $"{assemblyName.Name}.dll";
            var dllFile = _extension.FileProvider.GetFileInfo(dll);

            if (dllFile.Exists)
            {
                using var dllStream = GetSeekableStream(dllFile);

                var pdb = $"{assemblyName.Name}.pdb";
                var pdbFile = _extension.FileProvider.GetFileInfo(pdb);

                if (pdbFile.Exists)
                {
                    using var pdbStream = GetSeekableStream(pdbFile);
                    return LoadFromStream(dllStream, pdbStream);
                }
                else
                {
                    return LoadFromStream(dllStream);
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
