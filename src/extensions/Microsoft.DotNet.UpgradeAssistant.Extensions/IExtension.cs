// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IExtension
    {
        string Name { get; }

        T? GetOptions<T>(string sectionName);

        Stream? GetFile(string path);

        IEnumerable<string> GetFiles(string path, string searchPattern);

        IEnumerable<string> GetFiles(string path);
    }
}
