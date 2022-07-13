// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using NuGet.Frameworks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public sealed class ApiFrameworkAvailability
{
    public ApiFrameworkAvailability(NuGetFramework framework, ApiDeclarationModel declaration, PackageModel? package, NuGetFramework? packageFramework)
    {
        Framework = framework;
        Declaration = declaration;
        Package = package;
        PackageFramework = packageFramework;
    }

    [MemberNotNullWhen(false, nameof(Package))]
    public bool IsInBox => Package is null;

    public NuGetFramework Framework { get; }

    public ApiDeclarationModel Declaration { get; }

    public PackageModel? Package { get; }

    public NuGetFramework? PackageFramework { get; }
}
