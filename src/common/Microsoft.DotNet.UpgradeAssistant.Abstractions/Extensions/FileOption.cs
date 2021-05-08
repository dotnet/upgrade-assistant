// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class FileOption<T>
    {
        public IFileProvider Files { get; set; } = default!;

        public T Value { get; set; } = default!;
    }
}
