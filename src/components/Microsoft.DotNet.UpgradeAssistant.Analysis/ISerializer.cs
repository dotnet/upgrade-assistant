// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public interface ISerializer
    {
        void Write<T>(TextWriter writer, T obj);
    }
}
