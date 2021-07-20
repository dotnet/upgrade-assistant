// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    public interface IEntrypointResolver
    {
        IEnumerable<IProject> GetEntrypoints(IEnumerable<IProject> projects, IReadOnlyCollection<string> names);
    }
}
