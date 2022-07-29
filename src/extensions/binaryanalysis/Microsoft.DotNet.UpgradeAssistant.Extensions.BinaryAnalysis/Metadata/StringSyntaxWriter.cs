// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

internal sealed class StringSyntaxWriter : SyntaxWriter, IDisposable
{
    private readonly StringWriter _stringWriter;
    private readonly IndentedTextWriter _writer;

    public StringSyntaxWriter()
    {
        _stringWriter = new StringWriter();
        _writer = new IndentedTextWriter(_stringWriter);
    }

    public override int Indent
    {
        get => _writer.Indent;
        set => _writer.Indent = value;
    }

    public override void WriteKeyword(string text)
    {
        _writer.Write(text);
    }

    public override void WritePunctuation(string text)
    {
        _writer.Write(text);
    }

    public override void WriteReference(ISymbol symbol, string text)
    {
        _writer.Write(text);
    }

    public override void WriteLiteralString(string text)
    {
        _writer.Write(text);
    }

    public override void WriteLiteralNumber(string text)
    {
        _writer.Write(text);
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
