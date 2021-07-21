// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public enum TargetSyntaxType
    {
        /// <summary>
        /// Syntax representing a namespace reference.
        /// </summary>
        Namespace,

        /// <summary>
        /// Syntax representing a type or interface.
        /// </summary>
        Type,

        /// <summary>
        /// Syntax representing a method, property, field, or event.
        /// </summary>
        Member
    }
}
