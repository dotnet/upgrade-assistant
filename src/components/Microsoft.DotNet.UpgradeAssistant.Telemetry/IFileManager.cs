// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
