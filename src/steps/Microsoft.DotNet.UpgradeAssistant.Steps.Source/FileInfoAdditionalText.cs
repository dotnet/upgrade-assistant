// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source
{
    internal class FileInfoAdditionalText : AdditionalText
    {
        private readonly IFileInfo _file;

        public FileInfoAdditionalText(IFileInfo file)
        {
            _file = file;
        }

        public override string Path => _file.PhysicalPath ?? _file.Name;

        public override SourceText? GetText(CancellationToken cancellationToken = default)
        {
            using var stream = _file.CreateReadStream();
            return SourceText.From(stream);
        }
    }
}
