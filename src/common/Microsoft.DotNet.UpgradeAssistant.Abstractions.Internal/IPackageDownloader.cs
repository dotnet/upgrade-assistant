﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Extensions;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IPackageDownloader
    {
        Task<bool> DownloadPackageToDirectoryAsync(string path, NuGetReference nugetReference, string source, CancellationToken token);

        ValueTask<NuGetReference?> GetNuGetReference(string name, string? version, string source, CancellationToken token);

        bool CreateArchive(IExtensionInstance extension, Stream stream);
    }
}
