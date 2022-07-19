// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

internal sealed class MarkupSyntaxWriter : SyntaxWriter, IDisposable
{
    private readonly StringWriter _stringWriter;
    private readonly IndentedTextWriter _writer;

    public MarkupSyntaxWriter()
    {
        _stringWriter = new StringWriter();
        _writer = new IndentedTextWriter(_stringWriter);
    }

    public override int Indent
    {
        get => _writer.Indent;
        set => _writer.Indent = value;
    }

    private void Write(string text)
    {
        _writer.Write(new XText(text).ToString());
    }

    public override void WriteKeyword(string text)
    {
        _writer.Write("<k>");
        Write(text);
        _writer.Write("</k>");
    }

    public override void WriteLiteralNumber(string text)
    {
        _writer.Write("<n>");
        Write(text);
        _writer.Write("</n>");
    }

    public override void WriteLiteralString(string text)
    {
        _writer.Write("<s>");
        Write(text);
        _writer.Write("</s>");
    }

    public override void WritePunctuation(string text)
    {
        _writer.Write("<p>");
        Write(text);
        _writer.Write("</p>");
    }

    public override void WriteReference(ISymbol symbol, string text)
    {
        var guid = symbol?.GetCatalogGuid() ?? Guid.Empty;
        if (guid == Guid.Empty)
        {
            Write(text);
        }
        else
        {
            _writer.Write("<r i=\"");
            _writer.Write(guid.ToString("N"));
            _writer.Write("\">");
            Write(text);
            _writer.Write("</r>");
        }
    }

    public override void WriteSpace()
    {
        _writer.Write(" ");
    }

    public override void WriteLine()
    {
        _writer.WriteLine();
    }

    public override string ToString()
    {
        _writer.Flush();
        return _stringWriter.ToString();
    }

    public void Dispose()
    {
        _stringWriter.Dispose();
        _writer.Dispose();
    }
}
