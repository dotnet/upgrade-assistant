// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    public class WindowsUtilities
    {
        public async Task<bool> IsWinFormsProjectAsync(IProject project, CancellationToken token)
        {
            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            return components.HasFlag(ProjectComponents.WinForms);
        }
    }
}
