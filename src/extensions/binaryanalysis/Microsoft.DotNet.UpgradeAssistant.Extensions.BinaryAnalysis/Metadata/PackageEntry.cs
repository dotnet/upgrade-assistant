// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public sealed class PackageEntry
{
    public static PackageEntry Create(string id, string version, IReadOnlyList<FrameworkEntry> entries)
    {
        return new PackageEntry(id, version, entries);
    }

    private PackageEntry(string id, string version, IReadOnlyList<FrameworkEntry> entries)
    {
        Fingerprint = CatalogExtensions.GetCatalogGuid(id, version);
        Id = id;
        Version = version;
        Entries = entries;
    }

    public Guid Fingerprint { get; }

    public string Id { get; }

    public string Version { get; }

    public IReadOnlyList<FrameworkEntry> Entries { get; }

    public void Write(Stream stream)
    {
        XmlEntryFormat.WritePackageEntry(stream, this);
    }

    public XDocument ToDocument()
    {
        using var stream = new MemoryStream();
        Write(stream);
        stream.Position = 0;
        return XDocument.Load(stream);
    }
}
