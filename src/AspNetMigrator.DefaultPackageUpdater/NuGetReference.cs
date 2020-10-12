using System;
using System.Collections.Generic;

namespace AspNetMigrator.Engine
{
    public class NuGetReference : IEquatable<NuGetReference>
    {
        public NuGetReference() { }

        public NuGetReference(string name, string version)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version;
        }

        public NuGetReference(string name, Version version)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version?.ToString();
        }

        public string Name { get; set; }
        public string Version { get; set; }

        public bool HasWildcardVersion => Version.Equals("*", StringComparison.OrdinalIgnoreCase);

        public Version GetVersion()
        {
            if (HasWildcardVersion)
            {
                return null;
            }

            return System.Version.TryParse(Version, out var parsedVersion) ? parsedVersion : new Version(0,0,0,0);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as NuGetReference);
        }

        public bool Equals(NuGetReference other)
        {
            return other != null &&
                   Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) &&
                   Version.Equals(other.Version, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Version);
        }

        public static bool operator ==(NuGetReference left, NuGetReference right)
        {
            return EqualityComparer<NuGetReference>.Default.Equals(left, right);
        }

        public static bool operator !=(NuGetReference left, NuGetReference right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{Name}, Version={Version}";
        }
    }
}
