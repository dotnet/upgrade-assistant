// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.VisualBasic
{
    /// <summary>
    /// An implementation of <see cref="ITargetFrameworkSelectorFilter"/> that adapts the target framework selection based
    /// on usage of <c>MyType</c> in Visual Basic projects.
    /// </summary>
    /// <see cref="https://docs.microsoft.com/en-us/dotnet/visual-basic/developing-apps/development-with-my/how-my-depends-on-project-type"/>
    public class MyTypeTargetFrameworkSelectorFilter : ITargetFrameworkSelectorFilter
    {
        private readonly ILogger<MyTypeTargetFrameworkSelectorFilter> _logger;
        private readonly HashSet<string> _windowsMyTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Windows",
            "WindowsForms",
        };

        public MyTypeTargetFrameworkSelectorFilter(ILogger<MyTypeTargetFrameworkSelectorFilter> logger)
        {
            _logger = logger;
        }

        public void Process(ITargetFrameworkSelectorFilterState tfm)
        {
            if (tfm is null)
            {
                throw new ArgumentNullException(nameof(tfm));
            }

            var myType = tfm.Project.GetFile().GetPropertyValue("MyType");

            if (_windowsMyTypes.Contains(myType))
            {
                var final = tfm.AppBase with { Platform = TargetFrameworkMoniker.Platforms.Windows };
                _logger.LogInformation("Project {Name} contains MyType node that requires at least {framework}", tfm.Project, final);

                tfm.TryUpdate(final);
            }
        }
    }
}
