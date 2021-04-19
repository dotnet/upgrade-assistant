// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record TargetFrameworkMoniker(string Framework, Version FrameworkVersion) : IEquatable<TargetFrameworkMoniker>
    {
        private const string NetStandard = "netstandard";
        private const string NetFramework = "netframework";
        private const string Net = "net";
        private const string NetCoreApp = "netcoreapp";

#pragma warning disable CA1707 // Identifiers should not contain underscores
        public static readonly TargetFrameworkMoniker NetStandard10 = new TargetFrameworkMoniker(NetStandard, new Version(1, 0)).Normalize();
        public static readonly TargetFrameworkMoniker NetStandard20 = NetStandard10 with { FrameworkVersion = new Version(2, 0) };
        public static readonly TargetFrameworkMoniker NetStandard21 = NetStandard20 with { FrameworkVersion = new Version(2, 1) };
        public static readonly TargetFrameworkMoniker NetCoreApp21 = new(NetCoreApp, new Version(2, 1));
        public static readonly TargetFrameworkMoniker NetCoreApp30 = NetCoreApp21 with { FrameworkVersion = new Version(3, 0) };
        public static readonly TargetFrameworkMoniker NetCoreApp31 = NetCoreApp30 with { FrameworkVersion = new Version(3, 1) };
        public static readonly TargetFrameworkMoniker Net45 = new TargetFrameworkMoniker(Net, new Version(4, 5)).Normalize();
        public static readonly TargetFrameworkMoniker Net46 = Net45 with { FrameworkVersion = new Version(4, 6) };
        public static readonly TargetFrameworkMoniker Net461 = Net45 with { FrameworkVersion = new Version(4, 6, 1) };
        public static readonly TargetFrameworkMoniker Net462 = Net45 with { FrameworkVersion = new Version(4, 6, 2) };
        public static readonly TargetFrameworkMoniker Net47 = Net45 with { FrameworkVersion = new Version(4, 7) };
        public static readonly TargetFrameworkMoniker Net471 = Net45 with { FrameworkVersion = new Version(4, 7, 1) };
        public static readonly TargetFrameworkMoniker Net472 = Net45 with { FrameworkVersion = new Version(4, 7, 2) };
        public static readonly TargetFrameworkMoniker Net48 = Net45 with { FrameworkVersion = new Version(4, 8) };
        public static readonly TargetFrameworkMoniker Net50 = NetCoreApp30 with { Framework = Net, FrameworkVersion = new Version(5, 0) };
        public static readonly TargetFrameworkMoniker Net50_Windows = Net50 with { Platform = Platforms.Windows };
        public static readonly TargetFrameworkMoniker Net60 = Net50 with { FrameworkVersion = new Version(6, 0) };
        public static readonly TargetFrameworkMoniker Net60_Windows = Net60 with { Platform = Platforms.Windows };
#pragma warning restore CA1707 // Identifiers should not contain underscores

#pragma warning disable CA1034 // Nested types should not be visible
        public static class Platforms
        {
            public const string Windows = "windows";
        }
#pragma warning restore CA1034 // Nested types should not be visible

        public string? Platform { get; init; }

        public Version? PlatformVersion { get; init; }

        public string Name => ToString();

        public virtual bool Equals(TargetFrameworkMoniker? other)
        {
            if (other is null)
            {
                return false;
            }

            var @this = Normalize();
            var tfm = other.Normalize();

            return string.Equals(@this.Framework, tfm.Framework, StringComparison.OrdinalIgnoreCase)
                && Equals(@this.FrameworkVersion, tfm.FrameworkVersion)
                && string.Equals(@this.Platform, tfm.Platform, StringComparison.OrdinalIgnoreCase)
                && Equals(@this.PlatformVersion, tfm.PlatformVersion);
        }

        public override int GetHashCode()
        {
            var normalized = Normalize();
            var hashcode = default(HashCode);

            hashcode.Add(normalized.Framework, StringComparer.OrdinalIgnoreCase);
            hashcode.Add(normalized.FrameworkVersion);
            hashcode.Add(normalized.Platform, StringComparer.OrdinalIgnoreCase);
            hashcode.Add(normalized.PlatformVersion);

            return hashcode.ToHashCode();
        }

        public override string ToString()
        {
            var normalized = Normalize();
            var sb = new StringBuilder();

            sb.Append(normalized.Framework);
            sb.Append(normalized.FrameworkVersion);

            if (IsFramework)
            {
                sb.Replace(".", string.Empty);
            }
            else if (IsNet50OrAbove && normalized.Platform is not null)
            {
                sb.Append('-');
                sb.Append(normalized.Platform);

                if (PlatformVersion is not null)
                {
                    sb.Append(normalized.PlatformVersion);
                }
            }

            return sb.ToString();
        }

        public bool IsFramework
        {
            get
            {
                if (Framework.Equals(Net, StringComparison.OrdinalIgnoreCase) && !Is50OrAbove)
                {
                    return true;
                }

                return Framework.ToUpperInvariant().Contains(NetFramework.ToUpperInvariant());
            }
        }

        public bool IsNetStandard => Framework.ToUpperInvariant().Contains(NetStandard.ToUpperInvariant());

        private bool IsNet50OrAbove => (IsNetCoreApp || Framework.Equals(Net, StringComparison.OrdinalIgnoreCase)) && Is50OrAbove;

        private bool Is50OrAbove => FrameworkVersion.Major >= 5;

        private bool IsNetCoreApp => Framework.ToUpperInvariant().Contains(NetCoreApp.ToUpperInvariant());

        public bool IsNetCore => IsNetCoreApp || IsNet50OrAbove;

        public bool IsWindows => Platform?.Equals(Platforms.Windows, StringComparison.OrdinalIgnoreCase) ?? false;

        private bool IsNormalized { get; init; }

        /// <summary>
        /// Normalizes the names and versions used to ensure that comparisons will be accurate.
        /// </summary>
        /// <returns>A normalized instance. If the current instance is already normalized, it will be returned without any changes.</returns>
        public TargetFrameworkMoniker Normalize()
        {
            if (IsNormalized)
            {
                return this;
            }

            return this with
            {
                Framework = GetNormalizedFramework(),
                FrameworkVersion = NormalizeVersion(FrameworkVersion) ?? FrameworkVersion,
                Platform = GetNormalizedPlatform(),
                PlatformVersion = NormalizeVersion(PlatformVersion),
                IsNormalized = true,
            };

            string? GetNormalizedPlatform()
            {
                if (IsWindows)
                {
                    return Platforms.Windows;
                }

                if (string.IsNullOrEmpty(Platform))
                {
                    return null;
                }

                return Platform;
            }

            string GetNormalizedFramework()
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

            static Version? NormalizeVersion(Version? v) => v switch
            {
                null => null,
                (0, 0, 0, 0) => null,
                _ when v.Build > 0 && v.Revision > 0 => v,
                _ when v.Build > 0 => new Version(v.Major, v.Minor, v.Build),
                _ => new Version(v.Major, v.Minor),
            };
        }
    }
}
