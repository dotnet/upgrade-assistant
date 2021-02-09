using System;
using System.Text.RegularExpressions;

namespace AspNetMigrator
{
    public record TargetFrameworkMoniker(string Name)
    {
        private const string NetStandardNamePrefix = "netstandard";
        private const string NetPrefix = "net";

        public override string ToString() => Name;

        public bool IsNetStandard => Name.StartsWith(NetStandardNamePrefix, StringComparison.OrdinalIgnoreCase);

        public bool IsNetCore => Name.StartsWith(NetPrefix, StringComparison.OrdinalIgnoreCase) && Name.Contains('.');
    }
}
