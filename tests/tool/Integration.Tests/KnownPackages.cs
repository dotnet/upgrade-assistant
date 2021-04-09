// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Microsoft.DotNet.UpgradeAssistant;

namespace Integration.Tests
{
    namespace Integration.Tests
    {
        internal class KnownPackages
        {
            private const string EXPECTED_PACKAGE_VERSIONS = "ExpectedPackageVersions.json";
            private readonly Dictionary<string, string> _knownValues;

            public KnownPackages()
            {
                var knownVersionsJson = File.ReadAllText(EXPECTED_PACKAGE_VERSIONS);
                _knownValues = JsonSerializer.Deserialize<Dictionary<string, string>>(knownVersionsJson)
                    ?? throw new InvalidOperationException($"{EXPECTED_PACKAGE_VERSIONS} was not found");
            }

            public bool TryGetValue(string name, [MaybeNullWhen(false)] out NuGetReference nuget)
            {
                if (_knownValues is null)
                {
                    nuget = null;
                    return false;
                }

                if (_knownValues.TryGetValue(name, out var specificVersion))
                {
                    nuget = new NuGetReference(name, specificVersion.ToString());
                    return true;
                }

                nuget = null;
                return false;
            }
        }
    }
}
