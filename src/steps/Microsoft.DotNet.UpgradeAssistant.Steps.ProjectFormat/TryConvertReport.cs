// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Reporting;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat
{
    internal class TryConvertReport : ISectionGenerator
    {
        public Task<Section> GenerateContentAsync(IProject project, CancellationToken token)
        {
            var content = new List<Content>();

            if (!project.GetFile().IsSdk)
            {
                content.Add(new TextContent("Project needs to be updated to the SDK-style .csproj"));
            }

            if (project.NuGetReferences.PackageReferenceFormat == NugetPackageFormat.PackageConfig)
            {
                content.Add(new TextContent("NuGet packages must be updated to PackageReference"));
            }

            var section = new Section("Project File Status")
            {
                Content = content
            };

            return Task.FromResult(section);
        }
    }
}
