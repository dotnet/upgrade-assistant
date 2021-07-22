// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public interface IFileManager
    {
        bool FileExists(string path);

        bool DirectoryExists(string directoryPath);

        void CreateDirectory(string directoryPath);

        void WriteAllText(string path, string text);

        void WriteAllBytes(string path, byte[] bytes);

        string ReadAllText(string path);
    }
}
