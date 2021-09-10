// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Web
{
    public class WebComponentIdentifier : IComponentIdentifier
    {
        private const string WebApplicationTargets = "Microsoft.WebApplication.targets";
        private const string WebSdk = "Microsoft.NET.Sdk.Web";

        private readonly string[] _webFrameworkReferences = new[]
        {
            "Microsoft.AspNetCore.App"
        };

        private readonly string[] _webReferences = new[]
        {
            "System.Web",
            "System.Web.Abstractions",
            "System.Web.Mvc",
            "System.Web.Razor",
            "System.Web.Routing",
            "System.Web.WebPages"
        };

        public ValueTask<ProjectComponents> GetComponentsAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var file = project.GetFile();
            var components = ProjectComponents.None;

            var references = project.References.Select(r => r.Name);
            if ((file.Imports != null && file.Imports.Contains(WebApplicationTargets, StringComparer.OrdinalIgnoreCase)) ||
                references.Any(r => _webReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.AspNet;
            }

            if (file.IsSdk)
            {
                if (file.Sdk.Contains(WebSdk))
                {
                    components |= ProjectComponents.AspNetCore;
                }
                else
                {
                    var frameworkReferenceNames = project.FrameworkReferences.Select(r => r.Name);
                    if (frameworkReferenceNames.Any(f => _webFrameworkReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
                    {
                        components |= ProjectComponents.AspNetCore;
                    }
                }
            }

            return new ValueTask<ProjectComponents>(components);
        }
    }
}
