// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

using Microsoft.CodeAnalysis;

using NuGet.Protocol.Plugins;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public sealed class PreviewRequirementEntry
{
    private PreviewRequirementEntry(string? message, Uri url)
    {
        Message = message;
        Url = url;
    }

    public string? Message { get; }

    public Uri Url { get; }

    public static PreviewRequirementEntry? Create(ISymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.MatchesName(nameof(System),
                                                     nameof(System.Runtime),
                                                     nameof(System.Runtime.Versioning),
                                                     nameof(RequiresPreviewFeaturesAttribute)) == true)
            {
                string? message = null;

                if (attribute.ConstructorArguments.Length == 1)
                {
                    message = attribute.ConstructorArguments[0].Value as string;
                }

                var requiresPreviewFeaturesUrl = attribute.GetNamedArgument(nameof(RequiresPreviewFeaturesAttribute.Url));
                if (requiresPreviewFeaturesUrl is not null)
                {
                    return new PreviewRequirementEntry(message, new Uri(requiresPreviewFeaturesUrl));
                }
            }
        }

        return null;
    }
}
