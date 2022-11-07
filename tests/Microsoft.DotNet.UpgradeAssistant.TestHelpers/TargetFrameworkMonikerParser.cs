// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Moq;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class TargetFrameworkMonikerParser
    {
#pragma warning disable CA1707 // Identifiers should not contain underscores
        public const string NetCoreApp21 = "netcoreapp2.1";
        public const string NetCoreApp30 = "netcoreapp3.0";
        public const string NetCoreApp31 = "netcoreapp3.1";
        public const string NetStandard10 = "netstandard1.0";
        public const string NetStandard20 = "netstandard2.0";
        public const string NetStandard21 = "netstandard2.1";
        public const string Net45 = "net45";
        public const string Net46 = "net46";
        public const string Net461 = "net461";
        public const string Net462 = "net462";
        public const string Net47 = "net47";
        public const string Net471 = "net471";
        public const string Net48 = "net48";
        public const string Net50 = "net5.0";
        public const string Net50_Windows = "net5.0-windows";
        public const string Net50_Windows_10_0_5 = "net5.0-windows10.0.5";
        public const string Net50_Windows_10_1_5 = "net5.0-windows10.1.5";
        public const string Net50_Windows_10_0_19041_0 = "net5.0-windows10.0.19041.0";
        public const string Net60 = "net6.0";
        public const string Net60_Windows = "net6.0-windows";
        public const string Net60_Linux = "net6.0-linux";
        public const string Net60_Windows_10_0_5 = "net6.0-windows10.0.5";
        public const string Net60_Windows_10_1_5 = "net6.0-windows10.1.5";
        public const string Net60_Windows_10_0_19041_0 = "net6.0-windows10.0.19041.0";
        public const string Net60_Android = "net6.0-android";
        public const string Net60_iOS = "net6.0-ios";
        public const string Net70 = "net7.0";
        public const string Net70_Android = "net7.0-android";
        public const string Net70_iOS = "net7.0-ios";

        public const string STS = Net50;
        public const string Preview = Net60;
        public const string LTS = NetCoreApp31;

#pragma warning restore CA1707 // Identifiers should not contain underscores

        private static readonly Dictionary<string, TargetFrameworkMoniker> _map = new Dictionary<string, TargetFrameworkMoniker>(StringComparer.OrdinalIgnoreCase)
        {
            { NetCoreApp21, TargetFrameworkMoniker.NetCoreApp21 },
            { NetCoreApp30, TargetFrameworkMoniker.NetCoreApp30 },
            { NetCoreApp31, TargetFrameworkMoniker.NetCoreApp31 },
            { NetStandard10, TargetFrameworkMoniker.NetStandard10 },
            { NetStandard20, TargetFrameworkMoniker.NetStandard20 },
            { NetStandard21, TargetFrameworkMoniker.NetStandard21 },
            { Net45, TargetFrameworkMoniker.Net45 },
            { Net46, TargetFrameworkMoniker.Net46 },
            { Net461, TargetFrameworkMoniker.Net461 },
            { Net462, TargetFrameworkMoniker.Net462 },
            { Net47, TargetFrameworkMoniker.Net47 },
            { Net471, TargetFrameworkMoniker.Net471 },
            { Net48, TargetFrameworkMoniker.Net48 },
            { Net50, TargetFrameworkMoniker.Net50 },
            { Net50_Windows, TargetFrameworkMoniker.Net50_Windows },
            { Net50_Windows_10_0_5, TargetFrameworkMoniker.Net50_Windows with { PlatformVersion = new Version(10, 0, 5) } },
            { Net50_Windows_10_1_5, TargetFrameworkMoniker.Net50_Windows with { PlatformVersion = new Version(10, 1, 5) } },
            { Net50_Windows_10_0_19041_0, TargetFrameworkMoniker.Net50_Windows with { PlatformVersion = new Version(10, 0, 19041, 0) } },
            { Net60, TargetFrameworkMoniker.Net60 },
            { Net60_Windows, TargetFrameworkMoniker.Net60_Windows },
            { Net60_Linux, TargetFrameworkMoniker.Net60_Linux },
            { Net60_Windows_10_0_5, TargetFrameworkMoniker.Net60_Windows with { PlatformVersion = new Version(10, 0, 5) } },
            { Net60_Windows_10_1_5, TargetFrameworkMoniker.Net60_Windows with { PlatformVersion = new Version(10, 1, 5) } },
            { Net60_Windows_10_0_19041_0, TargetFrameworkMoniker.Net60_Windows with { PlatformVersion = new Version(10, 0, 19041, 0) } },
            { Net60_Android, TargetFrameworkMoniker.Net60_Android },
            { Net60_iOS, TargetFrameworkMoniker.Net60_iOS },
            { Net70, TargetFrameworkMoniker.Net70 },
            { Net70_Android, TargetFrameworkMoniker.Net70_Android },
            { Net70_iOS, TargetFrameworkMoniker.Net70_iOS },
        };

        [return: NotNullIfNotNull("input")]
        public static TargetFrameworkMoniker? ParseTfm(string? input) => input is null ? null : _map[input];

        public static void SetupTryParse(this Mock<ITargetFrameworkMonikerComparer> mock)
        {
            foreach (var (tfmString, tfm) in _map)
            {
                var result = tfm;
                mock.Setup(m => m.TryParse(tfmString, out result)).Returns(true);
            }
        }
    }
}
