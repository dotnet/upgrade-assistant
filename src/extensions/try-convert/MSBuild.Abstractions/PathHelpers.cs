// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace MSBuild.Abstractions
{
    public static class PathHelpers
    {
        public static string GetNativePath(string path)
        {
            if (Path.DirectorySeparatorChar == '/')
            {
                return path.Replace('\\', '/');
            }

            return path;
        }

        public static string GetIncludePath(string path)
        {
            if (Path.DirectorySeparatorChar == '/')
            {
                return path.Replace('/', '\\');
            }

            return path;
        }
    }
}
