// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public enum PlatformAnnotationKind
{
    None,
    Unrestricted,
    UnrestrictedExceptFor,
    RestrictedTo
}
