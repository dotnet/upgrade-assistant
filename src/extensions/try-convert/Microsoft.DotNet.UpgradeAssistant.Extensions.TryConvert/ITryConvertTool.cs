// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.TryConvert
{
    public interface ITryConvertTool
    {
        bool IsAvailable { get; }

        string Path { get; }

        string? Version { get; }

        Task<bool> RunAsync(IUpgradeContext context, IProject project, CancellationToken token);
    }
}
