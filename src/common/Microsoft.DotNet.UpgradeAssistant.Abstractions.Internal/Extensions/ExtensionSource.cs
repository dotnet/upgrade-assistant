// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public record ExtensionSource(string Name)
    {
        private static readonly string DefaultSource = string.Empty;

        private string? _source;

        [AllowNull]
        public string Source
        {
            get => string.IsNullOrEmpty(_source) ? DefaultSource : _source!;
            init => _source = value;
        }

        public string? Version { get; init; }
    }
}
