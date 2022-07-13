// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public sealed class AssemblyEntry
{
    private AssemblyEntry(AssemblyIdentity identity,
                          PlatformSupportEntry? platformSupportEntry,
                          PreviewRequirementEntry? previewRequirementEntry,
                          List<ApiEntry> apis)
    {
        Fingerprint = ComputeFingerprint(identity, apis);
        Identity = identity;
        PlatformSupportEntry = platformSupportEntry;
        PreviewRequirementEntry = previewRequirementEntry;
        Apis = apis;
    }

    public Guid Fingerprint { get; }

    public AssemblyIdentity Identity { get; }

    public PlatformSupportEntry? PlatformSupportEntry { get; }

    public PreviewRequirementEntry? PreviewRequirementEntry { get; }

    public IList<ApiEntry> Apis { get; }

    public static AssemblyEntry Create(IAssemblySymbol assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var identity = assembly.Identity;
        var platformSupportEntry = PlatformSupportEntry.Create(assembly.Modules.First()) ?? PlatformSupportEntry.Create(assembly);
        var previewRequirementEntry = PreviewRequirementEntry.Create(assembly.Modules.First()) ?? PreviewRequirementEntry.Create(assembly);
        var apis = GetApis(assembly);
        return new AssemblyEntry(identity, platformSupportEntry, previewRequirementEntry, apis);
    }

    private static List<ApiEntry> GetApis(IAssemblySymbol symbol)
    {
        var result = new List<ApiEntry>();
        var types = symbol.GetAllTypes()
            .Where(t => t.IsIncludedInCatalog())
            .GroupBy(t => t.ContainingNamespace, SymbolEqualityComparer.Default);

        foreach (var namespaceGroup in types)
        {
            var entry = ApiEntry.Create(namespaceGroup.Key!);
            result.Add(entry);

            foreach (var type in namespaceGroup)
            {
                AddType(entry, type);
            }
        }

        return result;
    }

    private static void AddType(ApiEntry parent, ITypeSymbol symbol)
    {
        if (!symbol.IsIncludedInCatalog())
        {
            return;
        }

        var apiEntry = ApiEntry.Create(symbol, parent);
        parent.Children.Add(apiEntry);

        foreach (var member in symbol.GetMembers())
        {
            AddMember(apiEntry, member);
        }
    }

    private static void AddMember(ApiEntry parent, ISymbol symbol)
    {
        if (symbol is INamedTypeSymbol type)
        {
            AddType(parent, type);
            return;
        }

        if (!symbol.IsIncludedInCatalog())
        {
            return;
        }

        // We don't want to include accessors at the type level; we'll add them as children
        // under properties and events.
        if (symbol.IsAccessor())
        {
            return;
        }

        var entry = ApiEntry.Create(symbol, parent);
        parent.Children.Add(entry);

        switch (symbol)
        {
            case IPropertySymbol property:
                AddOptionalAccessor(property.GetMethod, entry);
                AddOptionalAccessor(property.SetMethod, entry);
                break;
            case IEventSymbol @event:
                AddOptionalAccessor(@event.AddMethod, entry);
                AddOptionalAccessor(@event.RemoveMethod, entry);
                AddOptionalAccessor(@event.RaiseMethod, entry);
                break;
        }
    }

    private static void AddOptionalAccessor(IMethodSymbol? accessor, ApiEntry parent)
    {
        if (accessor is null)
        {
            return;
        }

        var entry = ApiEntry.Create(accessor, parent);
        parent.Children.Add(entry);
    }

    private static Guid ComputeFingerprint(AssemblyIdentity identity, List<ApiEntry> roots)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);

        writer.WriteLine(identity.ToString());
        WriteDeclarations(writer, roots);

        writer.Flush();

        stream.Position = 0;

#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
        using var md5 = MD5.Create();
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
        var hashBytes = md5.ComputeHash(stream);
        return new Guid(hashBytes);

        static void WriteDeclarations(TextWriter writer, IEnumerable<ApiEntry> roots)
        {
            foreach (var a in roots)
            {
                writer.WriteLine(a.Syntax);
                WriteDeclarations(writer, a.Children);
            }
        }
    }

    public IEnumerable<ApiEntry> AllApis()
    {
        var stack = new Stack<ApiEntry>();
        foreach (var api in Apis.AsEnumerable().Reverse())
        {
            stack.Push(api);
        }

        while (stack.Count > 0)
        {
            var api = stack.Pop();
            yield return api;

            foreach (var child in api.Children.AsEnumerable().Reverse())
            {
                stack.Push(child);
            }
        }
    }
}
