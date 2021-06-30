// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public interface ISerializer
    {
        T Deserialize<T>(string content);

        string Serialize<T>(T obj);

        T Read<T>(string filePath);

        void Write<T>(string filePath, T obj);
    }
}
