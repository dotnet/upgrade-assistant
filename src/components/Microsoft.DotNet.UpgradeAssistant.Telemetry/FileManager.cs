// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
