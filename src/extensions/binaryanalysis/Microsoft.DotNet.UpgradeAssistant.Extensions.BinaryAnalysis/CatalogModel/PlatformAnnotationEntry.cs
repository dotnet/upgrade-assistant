// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public readonly struct PlatformAnnotationEntry : IEquatable<PlatformAnnotationEntry>
{
    public PlatformAnnotationEntry(string name, PlatformSupportRange range)
    {
        Name = name;
        Range = range;
    }

    public string Name { get; }

    public PlatformSupportRange Range { get; }

    public override string ToString()
    {
        if (Range.IsEmpty || Range.AllVersions)
        {
            return FormatPlatform(Name);
        }

        return $"{FormatPlatform(Name)} {Range}";
    }

    private static string FormatPlatform(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "android" => "Android",
            "browser" => "Browser",
            "freebsd" => "FreeBSD",
            "illumos" => "illumos",
            "ios" => "iOS",
            "linux" => "Linux",
            "maccatalyst" => "Mac Catalyst",
            "macos" => "macOS",
            "solaris" => "Solaris",
            "tvos" => "tvOS",
            "watchos" => "watchOS",
            "windows" => "Windows",
            _ => name
        };
    }

    public bool Equals(PlatformAnnotationEntry other)
    {
        return Name == other.Name &&
               Range.Equals(other.Range);
    }

    public override bool Equals(object? obj)
    {
        return obj is PlatformAnnotationEntry other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Range);
    }

    public static bool operator ==(PlatformAnnotationEntry left, PlatformAnnotationEntry right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PlatformAnnotationEntry left, PlatformAnnotationEntry right)
    {
        return !left.Equals(right);
    }
}
