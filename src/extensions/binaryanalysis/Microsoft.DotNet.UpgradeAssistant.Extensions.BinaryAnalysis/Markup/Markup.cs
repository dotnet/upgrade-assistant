// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text;
using System.Xml;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public class CatalogMarkup
{
    public CatalogMarkup(IEnumerable<MarkupPart> parts)
    {
        Parts = parts.ToImmutableArray();
    }

    public ImmutableArray<MarkupPart> Parts { get; }

    public static CatalogMarkup Parse(string text)
    {
        var settings = new XmlReaderSettings
        {
            ConformanceLevel = ConformanceLevel.Auto
        };
        using var stringReader = new StringReader(text);
        using var reader = XmlReader.Create(stringReader, settings);

        var parts = new List<MarkupPart>();
        var kind = (MarkupPartKind?)null;
        var reference = (Guid?)null;
        var sb = new StringBuilder();

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (sb.Length > 0)
                {
                    TokenizeWhitespaces(sb, parts);
                    sb.Clear();
                }

                if (reader.LocalName == "p")
                {
                    kind = MarkupPartKind.Punctuation;
                }
                else if (reader.LocalName == "k")
                {
                    kind = MarkupPartKind.Keyword;
                }
                else if (reader.LocalName == "n")
                {
                    kind = MarkupPartKind.LiteralNumber;
                }
                else if (reader.LocalName == "s")
                {
                    kind = MarkupPartKind.LiteralString;
                }
                else if (reader.LocalName == "r")
                {
                    var id = reader.GetAttribute("i");
                    if (id != null)
                    {
                        reference = Guid.Parse(id);
                    }

                    kind = MarkupPartKind.Reference;
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement)
            {
                if (kind != null)
                {
                    parts.Add(new MarkupPart(kind.Value, sb.ToString(), reference));
                }

                kind = null;
                reference = null;
                sb.Clear();
            }
            else if (reader.NodeType == XmlNodeType.Text ||
                     reader.NodeType == XmlNodeType.Whitespace ||
                     reader.NodeType == XmlNodeType.SignificantWhitespace)
            {
                sb.Append(reader.Value);
            }
        }

        return new CatalogMarkup(parts);

        static void TokenizeWhitespaces(StringBuilder sb, List<MarkupPart> parts)
        {
            var p = 0;
            while (p < sb.Length)
            {
                var c = sb[p];
                var l = p == sb.Length - 1 ? '\0' : sb[p + 1];
                var lineBreakWidth = (c == '\r' && l == '\n')
                                        ? 2
                                        : c == '\r' || c == '\n'
                                            ? 1
                                            : 0;

                if (lineBreakWidth > 0)
                {
                    parts.Add(new MarkupPart(MarkupPartKind.Whitespace, Environment.NewLine));
                    p += lineBreakWidth;
                }
                else
                {
                    parts.Add(new MarkupPart(MarkupPartKind.Whitespace, sb[p].ToString()));
                    p += 1;
                }
            }
        }
    }

    public override string ToString()
    {
        return string.Concat(Parts.Select(p => p.Text));
    }
}
