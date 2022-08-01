// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

internal static class XmlEntryFormat
{
    public static void WriteFrameworkEntry(Stream stream, FrameworkEntry frameworkEntry)
    {
        var document = new XDocument();
        var root = new XElement("framework", new XAttribute("name", frameworkEntry.FrameworkName));
        document.Add(root);

        var dictionary = new HashSet<Guid>();

        foreach (var assembly in frameworkEntry.Assemblies)
        {
            foreach (var api in assembly.AllApis())
            {
                if (dictionary.Add(api.Fingerprint))
                {
                    AddApi(root, api);
                }
            }
        }

        foreach (var assembly in frameworkEntry.Assemblies)
        {
            AddAssembly(root, assembly);
        }

        document.Save(stream);
    }

    public static void WritePackageEntry(Stream stream, PackageEntry packageEntry)
    {
        var document = new XDocument();
        var root = new XElement("package",
            new XAttribute("fingerprint", packageEntry.Fingerprint),
            new XAttribute("id", packageEntry.Id),
            new XAttribute("name", packageEntry.Version));
        document.Add(root);

        var dictionary = new HashSet<Guid>();

        foreach (var fx in packageEntry.Entries)
        {
            foreach (var assembly in fx.Assemblies)
            {
                foreach (var api in assembly.AllApis())
                {
                    if (dictionary.Add(api.Fingerprint))
                    {
                        AddApi(root, api);
                    }
                }
            }
        }

        foreach (var fx in packageEntry.Entries)
        {
            foreach (var assembly in fx.Assemblies)
            {
                AddAssembly(root, assembly, fx.FrameworkName);
            }
        }

        document.Save(stream);
    }

    private static void AddAssembly(XContainer parent, AssemblyEntry assembly, string? frameworkName = null)
    {
        var assemblyElement = new XElement("assembly",
            frameworkName is null ? null : new XAttribute("fx", frameworkName),
            new XAttribute("fingerprint", assembly.Fingerprint.ToString("N")),
            new XAttribute("name", assembly.Identity.Name),
            new XAttribute("publicKeyToken", assembly.Identity.GetPublicKeyTokenString()),
            new XAttribute("version", assembly.Identity.Version.ToString()));

        if (assembly.PlatformSupportEntry is not null)
        {
            AddPlatformSupport(assemblyElement, assembly.PlatformSupportEntry);
        }

        if (assembly.PreviewRequirementEntry is not null)
        {
            AddPreviewRequirement(assemblyElement, assembly.PreviewRequirementEntry);
        }

        parent.Add(assemblyElement);

        foreach (var api in assembly.AllApis())
        {
            var fingerprint = api.Fingerprint.ToString("N");

            var syntaxElement = new XElement("syntax", new XAttribute("id", fingerprint));
            assemblyElement.Add(syntaxElement);
            syntaxElement.Add(api.Syntax);

            if (api.ObsoletionEntry is not null)
            {
                AddObsoletion(assemblyElement, api.ObsoletionEntry, fingerprint);
            }

            if (api.PlatformSupportEntry is not null)
            {
                AddPlatformSupport(assemblyElement, api.PlatformSupportEntry, fingerprint);
            }

            if (api.PreviewRequirementEntry is not null)
            {
                AddPreviewRequirement(assemblyElement, api.PreviewRequirementEntry, fingerprint);
            }
        }
    }

    private static void AddApi(XContainer parent, ApiEntry api)
    {
        var apiElement = new XElement("api",
            new XAttribute("fingerprint", api.Fingerprint.ToString("N")),
            new XAttribute("kind", (int)api.Kind),
            new XAttribute("name", api.Name!));
        parent.Add(apiElement);

        if (api.Parent != null)
        {
            apiElement.Add(new XAttribute("parent", api.Parent.Fingerprint.ToString("N")));
        }
    }

    private static void AddObsoletion(XContainer parent, ObsoletionEntry obsoletion, string apiFingerprint)
    {
        var obsoletionElement = new XElement("obsolete",
            new XAttribute("id", apiFingerprint),
            new XAttribute("isError", obsoletion.IsError));

        if (obsoletion.Message is not null)
        {
            obsoletionElement.Add(new XAttribute("message", obsoletion.Message));
        }

        if (obsoletion.DiagnosticId is not null)
        {
            obsoletionElement.Add(new XAttribute("diagnosticId", obsoletion.DiagnosticId));
        }

        if (obsoletion.UrlFormat is not null)
        {
            obsoletionElement.Add(new XAttribute("urlFormat", obsoletion.UrlFormat));
        }

        parent.Add(obsoletionElement);
    }

    private static void AddPlatformSupport(XContainer parent, PlatformSupportEntry platformSupport, string? apiFingerprint = null)
    {
        foreach (var supported in platformSupport.SupportedPlatforms)
        {
            var supportedElement = new XElement("supportedPlatform",
                apiFingerprint is null ? null : new XAttribute("id", apiFingerprint),
                new XAttribute("name", supported));
            parent.Add(supportedElement);
        }

        foreach (var unsupported in platformSupport.UnsupportedPlatforms)
        {
            var unsupportedElement = new XElement("unsupportedPlatform",
                apiFingerprint is null ? null : new XAttribute("id", apiFingerprint),
                new XAttribute("name", unsupported));
            parent.Add(unsupportedElement);
        }
    }

    private static void AddPreviewRequirement(XContainer parent, PreviewRequirementEntry previewRequirement, string? apiFingerprint = null)
    {
        var previewRequirementElement = new XElement("previewRequirement",
            apiFingerprint is null ? null : new XAttribute("id", apiFingerprint));

        if (previewRequirement.Message is not null)
        {
            previewRequirementElement.Add(new XAttribute("message", previewRequirement.Message));
        }

        if (previewRequirement.Url is not null)
        {
            previewRequirementElement.Add(new XAttribute("url", previewRequirement.Url));
        }

        parent.Add(previewRequirementElement);
    }
}
