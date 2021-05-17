// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.VisualBasic
{
    /// <summary>
    /// An implementation of <see cref="ITargetFrameworkSelectorFilter"/> that adapts the target framework selection based
    /// on usage of <c>MyType</c> in Visual Basic projects.
    /// </summary>
    /// <see cref="https://docs.microsoft.com/en-us/dotnet/visual-basic/developing-apps/development-with-my/how-my-depends-on-project-type"/>
    public class MyNamespaceTargetFrameworkSelectorFilter : ITargetFrameworkSelectorFilter
    {
        private readonly ILogger<MyNamespaceTargetFrameworkSelectorFilter> _logger;
        private readonly HashSet<string> _windowsMyTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Windows",
            "WindowsForms",
        };

        public MyNamespaceTargetFrameworkSelectorFilter(ILogger<MyNamespaceTargetFrameworkSelectorFilter> logger)
        {
            _logger = logger;
        }

        public void Process(ITargetFrameworkSelectorFilterState tfm)
        {
            var myType = tfm.Project.GetFile().GetPropertyValue("MyType");

            if (_windowsMyTypes.Contains(myType))
            {
                _logger.LogInformation("Project {Name} contains MyType node that requires at least net5.0-windows.", tfm.Project);

                tfm.TryUpdate(TargetFrameworkMoniker.Net50_Windows);
            }
        }
    }
}
