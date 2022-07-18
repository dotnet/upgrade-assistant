// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public sealed class ObsoletionEntry
{
    private ObsoletionEntry(string? message, bool isError, string? diagnosticId, string? urlFormat)
    {
        Message = message;
        IsError = isError;
        DiagnosticId = diagnosticId;
        UrlFormat = urlFormat;
    }

    public string? Message { get; }

    public bool IsError { get; }

    public string? DiagnosticId { get; }

#pragma warning disable CA1056 // URI-like properties should not be strings
    public string? UrlFormat { get; }
#pragma warning restore CA1056 // URI-like properties should not be strings

    public static ObsoletionEntry? Create(ISymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.MatchesName(nameof(System), nameof(ObsoleteAttribute)) == true)
            {
                string? message = null;
                var isError = false;

                if (attribute.ConstructorArguments.Length == 1)
                {
                    message = attribute.ConstructorArguments[0].Value as string;
                }
                else if (attribute.ConstructorArguments.Length == 2)
                {
                    message = attribute.ConstructorArguments[0].Value as string;
                    isError = attribute.ConstructorArguments[1].Value is true;
                }

                var diagnosticId = attribute.GetNamedArgument(nameof(ObsoleteAttribute.DiagnosticId));
                var urlFormat = attribute.GetNamedArgument(nameof(ObsoleteAttribute.UrlFormat));

                return new ObsoletionEntry(message, isError, diagnosticId, urlFormat);
            }
        }

        return null;
    }
}
