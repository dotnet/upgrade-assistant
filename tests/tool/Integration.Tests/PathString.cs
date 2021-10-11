// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Integration.Tests
{
    internal readonly struct PathString
    {
        private readonly string[] _split;

        public PathString(string path)
        {
            _split = path.Split('/', '\\');
        }

        public bool Contains(string pathPart)
            => _split.Contains(pathPart, StringComparer.OrdinalIgnoreCase);

        public static implicit operator PathString(string path) => new PathString(path);
    }
}
