// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    internal class FileManager : IFileManager
    {
        public void CreateDirectory(string directoryPath) => Directory.CreateDirectory(directoryPath);

        public bool DirectoryExists(string directoryPath) => Directory.Exists(directoryPath);

        public string ReadAllText(string path) => File.ReadAllText(path);

        public void WriteAllText(string path, string text) => File.WriteAllText(path, text);

        public bool FileExists(string path) => File.Exists(path);

        public void WriteAllBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);
    }
}
