// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface ITargetFrameworkMonikerComparer : IComparer<TargetFrameworkMoniker>
    {
        /// <summary>
        /// Returns true if another tfm is compatible with a first tfm. For example, IsCompatible(net45, net40) should return true because
        /// net40-targeted dependencies are compatibile with net45.
        /// </summary>
        /// <param name="tfm">The TFM of the dependent project.</param>
        /// <param name="other">The TFM of the dependency.</param>
        /// <returns>True if the dependency is compatible with the dependent TFM, false otherwise.</returns>
        bool IsCompatible(TargetFrameworkMoniker tfm, TargetFrameworkMoniker other);

        /// <summary>
        /// Merges two TFMs, while accounting for platforms and version, to a single version that can be targeted by both.
        /// </summary>
        /// <param name="tfm1">The first TFM.</param>
        /// <param name="tfm2">The second TFM.</param>
        /// <param name="result">A merged TFM. For example, passing <c>net5.0-windows</c> and <c>net6.0</c> will result in <c>net6.0-windows</c>.</param>
        /// <returns>Whether the merge was successful.</returns>
        bool TryMerge(TargetFrameworkMoniker tfm1, TargetFrameworkMoniker tfm2, [MaybeNullWhen(false)] out TargetFrameworkMoniker result);

        bool TryParse(string input, [MaybeNullWhen(false)] out TargetFrameworkMoniker tfm);
    }
}
