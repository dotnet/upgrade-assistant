// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Client;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly
{
    internal class TemporaryFileNuGetPackageLookupIndex : NuGetPackageLookupIndex
    {
        private readonly string _path;

        public TemporaryFileNuGetPackageLookupIndex(IFileInfo file)
            : this(CopyToFile(file))
        {
        }

        private TemporaryFileNuGetPackageLookupIndex(string path)
            : base(path)
        {
            _path = path;
        }

        public override void Dispose()
        {
            base.Dispose();
            File.Delete(_path);
        }

        private static string CopyToFile(IFileInfo file)
        {
            var path = Path.GetTempFileName();
            using var fs = File.OpenWrite(path);
            using var stream = file.CreateReadStream();
            stream.CopyTo(fs);

            return path;
        }
    }
}
