using System;
using System.Text.RegularExpressions;

namespace AspNetMigrator
{
    public record TargetFrameworkMoniker(string Name)
    {
        private const string NetStandardNamePrefix = "netstandard";
        private const string NetPrefix = "net";
        private static readonly Regex NetFxVersionRegex = new Regex(@"v(?'major'\d)\.(?'minor'\d)(\.(?'build'\d))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public override string ToString() => Name;

        public bool IsNetStandard => Name.StartsWith(NetStandardNamePrefix, StringComparison.OrdinalIgnoreCase);

        public bool IsNetCore => Name.StartsWith(NetPrefix, StringComparison.OrdinalIgnoreCase) && Name.Contains('.');

        /// <summary>
        /// Parses a .NET Framework version string (for example v4.7.2) into
        /// the equivalent TFM (net472 for that example).
        /// </summary>
        public static TargetFrameworkMoniker ParseNetFxVersion(string version)
        {
            var match = NetFxVersionRegex.Match(version);

            if (!match.Success)
            {
                throw new ArgumentException("Invalid .NET Framework version string", nameof(version));
            }

            if (!match.Groups.TryGetValue("major", out var majorVersion) || !match.Groups.TryGetValue("minor", out var minorVersion))
            {
                throw new ArgumentException("Invalid .NET Framework version string", nameof(version));
            }

            match.Groups.TryGetValue("build", out var buildVersion);

            var tfmName = $"{NetPrefix}{majorVersion.Value}{minorVersion.Value}{(buildVersion is null ? string.Empty : buildVersion.Value)}";

            if (tfmName.Equals("net30"))
            {
                // There is no net30 TFM
                // https://docs.microsoft.com/dotnet/standard/frameworks
                tfmName = "net35";
            }

            return new TargetFrameworkMoniker(tfmName);
        }
    }
}
