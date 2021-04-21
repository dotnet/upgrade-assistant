// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record TargetFrameworkMoniker
    {
        private const string NetStandard = "netstandard";
        private const string NetFramework = "netframework";
        private const string Net = "net";
        private const string NetCoreApp = "netcoreapp";

#pragma warning disable CA1707 // Identifiers should not contain underscores
        public static readonly TargetFrameworkMoniker NetStandard10 = new(NetStandard, new Version(1, 0));
        public static readonly TargetFrameworkMoniker NetStandard20 = NetStandard10 with { FrameworkVersion = new Version(2, 0) };
        public static readonly TargetFrameworkMoniker NetStandard21 = NetStandard20 with { FrameworkVersion = new Version(2, 1) };
        public static readonly TargetFrameworkMoniker NetCoreApp21 = new(NetCoreApp, new Version(2, 1));
        public static readonly TargetFrameworkMoniker NetCoreApp30 = NetCoreApp21 with { FrameworkVersion = new Version(3, 0) };
        public static readonly TargetFrameworkMoniker NetCoreApp31 = NetCoreApp30 with { FrameworkVersion = new Version(3, 1) };
        public static readonly TargetFrameworkMoniker Net45 = new(Net, new Version(4, 5));
        public static readonly TargetFrameworkMoniker Net46 = Net45 with { FrameworkVersion = new Version(4, 6) };
        public static readonly TargetFrameworkMoniker Net461 = Net45 with { FrameworkVersion = new Version(4, 6, 1) };
        public static readonly TargetFrameworkMoniker Net462 = Net45 with { FrameworkVersion = new Version(4, 6, 2) };
        public static readonly TargetFrameworkMoniker Net47 = Net45 with { FrameworkVersion = new Version(4, 7) };
        public static readonly TargetFrameworkMoniker Net471 = Net45 with { FrameworkVersion = new Version(4, 7, 1) };
        public static readonly TargetFrameworkMoniker Net472 = Net45 with { FrameworkVersion = new Version(4, 7, 2) };
        public static readonly TargetFrameworkMoniker Net48 = Net45 with { FrameworkVersion = new Version(4, 8) };
        public static readonly TargetFrameworkMoniker Net50 = NetCoreApp30 with { Framework = Net, FrameworkVersion = new Version(5, 0) };
        public static readonly TargetFrameworkMoniker Net50_Windows = Net50 with { Platform = Platforms.Windows };
        public static readonly TargetFrameworkMoniker Net50_Linux = Net50 with { Platform = Platforms.Linux };
        public static readonly TargetFrameworkMoniker Net60 = Net50 with { FrameworkVersion = new Version(6, 0) };
        public static readonly TargetFrameworkMoniker Net60_Linux = Net60 with { Platform = Platforms.Linux };
        public static readonly TargetFrameworkMoniker Net60_Windows = Net60 with { Platform = Platforms.Windows };
#pragma warning restore CA1707 // Identifiers should not contain underscores

#pragma warning disable CA1034 // Nested types should not be visible
        public static class Platforms
        {
            public const string Windows = "windows";
            public const string Linux = "linux";
        }
#pragma warning restore CA1034 // Nested types should not be visible

        private readonly string? _platform;
        private readonly Version? _platformVersion;
        private readonly string _framework;
        private readonly Version _frameworkVersion;

        public TargetFrameworkMoniker(string framework, Version frameworkVersion)
        {
            _framework = framework;
            _frameworkVersion = frameworkVersion;
        }

        public string Framework
        {
            get
            {
                if (IsFramework || IsNet50OrAbove)
                {
                    return Net;
                }
                else if (IsNetCoreApp)
                {
                    return NetCoreApp;
                }
                else if (IsNetStandard)
                {
                    return NetStandard;
                }
                else
                {
                    return Framework;
                }
            }

            init => _framework = value;
        }

        public Version FrameworkVersion
        {
            get => NormalizeVersion(_frameworkVersion) ?? _frameworkVersion;
            init => _frameworkVersion = value;
        }

        public string? Platform
        {
            get
            {
                if (IsWindows)
                {
                    return Platforms.Windows;
                }

                if (string.IsNullOrEmpty(_platform))
                {
                    return null;
                }

                return _platform;
            }

            init => _platform = value;
        }

        public Version? PlatformVersion
        {
            get => NormalizeVersion(_platformVersion);
            init => _platformVersion = value;
        }

        public string Name => ToString();

        public virtual bool Equals(TargetFrameworkMoniker? other)
        {
            if (other is null)
            {
                return false;
            }

            return string.Equals(Framework, other.Framework, StringComparison.OrdinalIgnoreCase)
                && Equals(FrameworkVersion, other.FrameworkVersion)
                && string.Equals(Platform, other.Platform, StringComparison.OrdinalIgnoreCase)
                && Equals(PlatformVersion, other.PlatformVersion);
        }

        public override int GetHashCode()
        {
            var hashcode = default(HashCode);

            hashcode.Add(Framework, StringComparer.OrdinalIgnoreCase);
            hashcode.Add(FrameworkVersion);
            hashcode.Add(Platform, StringComparer.OrdinalIgnoreCase);
            hashcode.Add(PlatformVersion);

            return hashcode.ToHashCode();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(Framework);
            sb.Append(FrameworkVersion);

            if (IsFramework)
            {
                sb.Replace(".", string.Empty);
            }
            else if (IsNet50OrAbove && Platform is string platform)
            {
                sb.Append('-');
                sb.Append(platform);

                if (PlatformVersion is Version platformVersion)
                {
                    sb.Append(platformVersion);
                }
            }

            return sb.ToString();
        }

        public bool IsFramework
        {
            get
            {
                if (_framework.Equals(Net, StringComparison.OrdinalIgnoreCase) && !Is50OrAbove)
                {
                    return true;
                }

                return _framework.ToUpperInvariant().Contains(NetFramework.ToUpperInvariant());
            }
        }

        public bool IsNetStandard => _framework.ToUpperInvariant().Contains(NetStandard.ToUpperInvariant());

        private bool IsNet50OrAbove => (IsNetCoreApp || _framework.Equals(Net, StringComparison.OrdinalIgnoreCase)) && Is50OrAbove;

        private bool Is50OrAbove => _frameworkVersion.Major >= 5;

        private bool IsNetCoreApp => _framework.ToUpperInvariant().Contains(NetCoreApp.ToUpperInvariant());

        public bool IsNetCore => IsNetCoreApp || IsNet50OrAbove;

        public bool IsWindows => _platform?.Equals(Platforms.Windows, StringComparison.OrdinalIgnoreCase) ?? false;

        private static Version? NormalizeVersion(Version? v) => v switch
        {
            null => null,
            (0, 0, 0, 0) => null,
            _ when v.Build > 0 && v.Revision > 0 => v,
            _ when v.Build > 0 => new Version(v.Major, v.Minor, v.Build),
            _ => new Version(v.Major, v.Minor),
        };
    }
}
