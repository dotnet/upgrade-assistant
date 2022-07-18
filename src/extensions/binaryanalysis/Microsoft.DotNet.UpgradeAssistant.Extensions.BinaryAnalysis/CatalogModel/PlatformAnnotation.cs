// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public readonly struct PlatformAnnotation : IEquatable<PlatformAnnotation>
{
    public static PlatformAnnotation None { get; } = new(PlatformAnnotationKind.None);

    public static PlatformAnnotation Unrestricted { get; } = new(PlatformAnnotationKind.Unrestricted);

    private PlatformAnnotation(PlatformAnnotationKind kind)
    {
        Kind = kind;
        Entries = Array.Empty<PlatformAnnotationEntry>();
    }

    public PlatformAnnotation(IReadOnlyList<PlatformAnnotationEntry> entries)
    {
        var isAllowList = entries.Any(t => t.Range.IsAllowList);

        Kind = isAllowList ? PlatformAnnotationKind.RestrictedTo : PlatformAnnotationKind.UnrestrictedExceptFor;
        Entries = entries;
    }

    public PlatformAnnotationKind Kind { get; }

    public IReadOnlyList<PlatformAnnotationEntry> Entries { get; }

    public bool IsSupported(string platformName)
    {
        if (Kind is PlatformAnnotationKind.None or PlatformAnnotationKind.Unrestricted)
        {
            return true;
        }

        var (name, version) = PlatformAnnotationContext.ParsePlatform(platformName);

        foreach (var entry in Entries)
        {
            if (string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Range.IsSupported(version);
            }
        }

        return Kind == PlatformAnnotationKind.UnrestrictedExceptFor;
    }

    public override string ToString()
    {
        switch (Kind)
        {
            case PlatformAnnotationKind.None:
                return $"The framework doesn't have platform annotations.";
            case PlatformAnnotationKind.Unrestricted:
                return $"The API is supported on any platform.";
            case PlatformAnnotationKind.UnrestrictedExceptFor:
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"The API is supported on any platform except for:");

                    foreach (var e in Entries)
                    {
                        sb.AppendLine(CultureInfo.InvariantCulture, $"- {e}");
                    }

                    return sb.ToString().TrimEnd();
                }

            case PlatformAnnotationKind.RestrictedTo:
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"The API is only supported on these platforms:");

                    foreach (var e in Entries)
                    {
                        sb.AppendLine(CultureInfo.InvariantCulture, $"- {e}");
                    }

                    return sb.ToString().TrimEnd();
                }

            default:
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
                throw new ArgumentException($"Unexpected kind {Kind}");
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
        }
    }

    public bool Equals(PlatformAnnotation other)
    {
        return Kind == other.Kind &&
               Entries.SequenceEqual(other.Entries);
    }

    public override bool Equals(object? obj)
    {
        return obj is PlatformAnnotation other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        var builder = default(HashCode);
        builder.Add(Kind);

        foreach (var e in Entries)
        {
            builder.Add(e);
        }

        return builder.ToHashCode();
    }

    public static bool operator ==(PlatformAnnotation left, PlatformAnnotation right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PlatformAnnotation left, PlatformAnnotation right)
    {
        return !left.Equals(right);
    }
}
