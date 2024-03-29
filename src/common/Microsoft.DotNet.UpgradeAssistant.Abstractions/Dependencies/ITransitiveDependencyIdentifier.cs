﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Dependencies
{
    public interface ITransitiveDependencyIdentifier
    {
        Task<TransitiveClosureCollection> GetTransitiveDependenciesAsync(IEnumerable<NuGetReference> packages, IEnumerable<TargetFrameworkMoniker> tfms, CancellationToken token);
    }
}
