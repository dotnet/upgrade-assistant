// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal class ExtensionAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly ExtensionInstance _extension;

        public ExtensionAssemblyLoadContext(ExtensionInstance extension)
            : base(extension.Name)
        {
            _extension = extension;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // If available in the default, we want to ensure that is used.
            var inDefault = Default.Assemblies.Any(a => string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.Ordinal));

            if (inDefault)
            {
                return null;
            }

            var dll = $"{assemblyName.Name}.dll";
            var file = _extension.FileProvider.GetFileInfo(dll);

            if (file.Exists)
            {
                using var stream = file.CreateReadStream();
                return LoadFromStream(stream);
            }

            return null;
        }
    }
}
