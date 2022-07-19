// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public readonly struct ApiDeclarationModel : IEquatable<ApiDeclarationModel>
{
    private readonly int _offset;

    internal ApiDeclarationModel(ApiModel api, int offset)
    {
        Api = api;
        _offset = offset;
    }

    public ApiCatalogModel Catalog => Api.Catalog;

    public ApiModel Api { get; }

    public AssemblyModel Assembly
    {
        get
        {
            var assemblyOffset = Api.Catalog.ApiTable.ReadInt32(_offset);
            return new AssemblyModel(Api.Catalog, assemblyOffset);
        }
    }

    public ObsoletionModel? Obsoletion
    {
        get
        {
            return Api.Catalog.GetObsoletion(Api.Id, Assembly.Id);
        }
    }

    public IEnumerable<PlatformSupportModel> PlatformSupport
    {
        get
        {
            return Api.Catalog.GetPlatformSupport(Api.Id, Assembly.Id);
        }
    }

    public PreviewRequirementModel? PreviewRequirement
    {
        get
        {
            return Api.Catalog.GetPreviewRequirement(Api.Id, Assembly.Id);
        }
    }

    public CatalogMarkup GetMyMarkup()
    {
        var markupOffset = Api.Catalog.ApiTable.ReadInt32(_offset + 4);
        return Api.Catalog.GetMarkup(markupOffset);
    }

    public CatalogMarkup GetMarkup()
    {
        var assembly = Assembly;
        var markups = Api.AncestorsAndSelf()
                         .Select(a => a.Declarations.Single(d => d.Assembly == assembly))
                         .Select(d => d.GetMyMarkup())
                         .ToList();
        markups.Reverse();

        var parts = new List<MarkupPart>();

        var indent = 0;

        foreach (var markup in markups)
        {
            if (indent > 0)
            {
                if (indent - 1 > 0)
                {
                    parts.Add(new MarkupPart(MarkupPartKind.Whitespace, new string(' ', 4 * (indent - 1))));
                }

                parts.Add(new MarkupPart(MarkupPartKind.Punctuation, "{"));
                parts.Add(new MarkupPart(MarkupPartKind.Whitespace, Environment.NewLine));
            }

            var needsIndent = true;

            foreach (var part in markup.Parts)
            {
                if (needsIndent)
                {
                    // Add indentation
                    parts.Add(new MarkupPart(MarkupPartKind.Whitespace, new string(' ', 4 * indent)));
                    needsIndent = false;
                }

                parts.Add(part);

                if (part.Kind == MarkupPartKind.Whitespace && part.Text is "\n" or "\r" or "\r\n")
                {
                    needsIndent = true;
                }
            }

            parts.Add(new MarkupPart(MarkupPartKind.Whitespace, Environment.NewLine));

            indent++;
        }

        for (var i = markups.Count - 1 - 1; i >= 0; i--)
        {
            if (i > 0)
            {
                parts.Add(new MarkupPart(MarkupPartKind.Whitespace, new string(' ', 4 * i)));
            }

            parts.Add(new MarkupPart(MarkupPartKind.Punctuation, "}"));
            parts.Add(new MarkupPart(MarkupPartKind.Whitespace, Environment.NewLine));
        }

        return new CatalogMarkup(parts);
    }

    public override bool Equals(object? obj)
    {
        return obj is ApiDeclarationModel model && Equals(model);
    }

    public bool Equals(ApiDeclarationModel other)
    {
        return Api == other.Api &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Api, _offset);
    }

    public static bool operator ==(ApiDeclarationModel left, ApiDeclarationModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ApiDeclarationModel left, ApiDeclarationModel right)
    {
        return !(left == right);
    }
}
