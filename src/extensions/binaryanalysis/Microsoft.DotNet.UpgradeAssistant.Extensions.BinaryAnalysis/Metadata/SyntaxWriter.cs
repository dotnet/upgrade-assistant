// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

internal abstract class SyntaxWriter
{
    public abstract void WriteKeyword(string text);

    public abstract void WritePunctuation(string text);

    public abstract void WriteReference(ISymbol symbol, string text);

    public abstract void WriteLiteralString(string text);

    public abstract void WriteLiteralNumber(string text);

    public abstract void WriteSpace();

    public abstract void WriteLine();

    public abstract int Indent { get; set; }
}
