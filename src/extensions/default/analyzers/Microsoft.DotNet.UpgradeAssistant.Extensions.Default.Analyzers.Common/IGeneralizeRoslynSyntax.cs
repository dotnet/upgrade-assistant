// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Common
{
    public interface IGeneralizeRoslynSyntax<TCsharp, TVisualBasic>
    {
        Location GetLocation();

        TCsharp GetCSharpNode();

        TVisualBasic GetVisualBasicNode();
    }
}
