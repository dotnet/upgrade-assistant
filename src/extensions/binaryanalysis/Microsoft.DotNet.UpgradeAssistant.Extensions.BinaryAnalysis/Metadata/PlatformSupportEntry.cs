// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public sealed class PlatformSupportEntry
{
    public IReadOnlyList<string> SupportedPlatforms { get; }

    public IReadOnlyList<string> UnsupportedPlatforms { get; }

    private PlatformSupportEntry(IReadOnlyList<string> supportedPlatforms, IReadOnlyList<string> unsupportedPlatforms)
    {
        SupportedPlatforms = supportedPlatforms;
        UnsupportedPlatforms = unsupportedPlatforms;
    }

    public static PlatformSupportEntry? Create(ISymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        List<string>? supportedPlatforms = null;
        List<string>? unsupportedPlatforms = null;

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.MatchesName(nameof(System),
                                                     nameof(System.Runtime),
                                                     nameof(System.Runtime.Versioning),
                                                     nameof(SupportedOSPlatformAttribute)) == true)
            {
                if (attribute.ConstructorArguments.Length == 1 &&
                    attribute.ConstructorArguments[0].Value is string argument)
                {
                    supportedPlatforms ??= new List<string>();
                    supportedPlatforms.Add(argument);
                }
            }

            if (attribute.AttributeClass?.MatchesName(nameof(System),
                                                     nameof(System.Runtime),
                                                     nameof(System.Runtime.Versioning),
                                                     nameof(UnsupportedOSPlatformAttribute)) == true)
            {
                if (attribute.ConstructorArguments.Length == 1 &&
                    attribute.ConstructorArguments[0].Value is string argument)
                {
                    unsupportedPlatforms ??= new List<string>();
                    unsupportedPlatforms.Add(argument);
                }
            }
        }

        if (supportedPlatforms is null && unsupportedPlatforms is null)
        {
            return null;
        }

        return new PlatformSupportEntry(supportedPlatforms?.ToArray() ?? Array.Empty<string>(),
                                        unsupportedPlatforms?.ToArray() ?? Array.Empty<string>());
    }
}
