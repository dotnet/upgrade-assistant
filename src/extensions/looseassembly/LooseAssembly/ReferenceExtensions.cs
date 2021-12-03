// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly
{
    public static class ReferenceExtensions
    {
        public static bool TryResolveHintPath(this IProject project, Reference reference, [MaybeNullWhen(false)] out string fullpath)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (reference is null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            var rawPath = reference.HintPath ?? reference.Name;
            var path = Path.GetFullPath(Path.Combine(project.FileInfo.Directory!.FullName, rawPath));

            if (File.Exists(path))
            {
                fullpath = path;
                return true;
            }

            fullpath = null;
            return false;
        }
    }
}
