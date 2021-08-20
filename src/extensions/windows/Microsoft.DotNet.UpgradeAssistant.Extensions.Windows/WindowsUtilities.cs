// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    public class WindowsUtilities
    {
        public WindowsUtilities()
        {
        }

        public async Task<bool> IsWinFormsProjectAsync(IProject project, CancellationToken token)
        {
            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            if (components.HasFlag(ProjectComponents.WinForms))
            {
                return true;
            }

            return false;
        }
    }
}
