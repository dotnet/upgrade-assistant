using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator.Reporting;

namespace AspNetMigrator
{
    internal class TryConvertReport : ISectionGenerator
    {
        public Task<Section> GenerateContentAsync(IProject project, CancellationToken token)
        {
            var content = new List<Content>();

            if (!project.GetFile().IsSdk)
            {
                content.Add(new Text("Project needs to be updated to new style csproj"));
            }

            if (project.PackageReferenceFormat != NugetPackageFormat.PackageReference)
            {
                content.Add(new Text("NuGet packages must be updated to PackageReference"));
            }

            var section = new Section("Project File Status")
            {
                Content = content
            };

            return Task.FromResult(section);
        }
    }
}
