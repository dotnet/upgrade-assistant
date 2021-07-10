// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public interface IJsonSerializer : ISerializer
    {
        JsonSerializerSettings Settings { get; set; }

        void Write<T>(string filePath, T obj, bool ensureDirectory);
    }
}
