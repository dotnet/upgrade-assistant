using System.Collections.Immutable;
using System.Linq;

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace MSBuild.Abstractions
{
    public class UnconfiguredProject
    {
        public ImmutableDictionary<string, IProject> ConfiguredProjects { get; private set; } = ImmutableDictionary<string, IProject>.Empty;

        public IProject FirstConfiguredProject => ConfiguredProjects.First().Value;

        public ImmutableDictionary<string, ImmutableDictionary<string, string>> Configurations { get; }

        public UnconfiguredProject(ImmutableDictionary<string, ImmutableDictionary<string, string>> configurations)
        {
            Configurations = configurations;
        }

        public void LoadProjects(ProjectCollection collection, ImmutableDictionary<string, string> globalProperties, string projectFilePath)
        {
            var projectBuilder = ImmutableDictionary.CreateBuilder<string, IProject>();
            foreach (var config in Configurations)
            {
                var globalPropertiesWithDimensions = globalProperties.AddRange(config.Value);
                var project = new MSBuildProject(collection.LoadProject(projectFilePath, globalPropertiesWithDimensions, toolsVersion: null));
                projectBuilder.Add(config.Key, project);
            }

            ConfiguredProjects = projectBuilder.ToImmutable();
        }

        public void LoadProjects(ProjectCollection collection, ProjectRootElement rootElement)
        {
            var projectBuilder = ImmutableDictionary.CreateBuilder<string, IProject>();
            foreach (var config in Configurations)
            {
                var project = new MSBuildProject(new Project(rootElement, config.Value, null, collection));
                projectBuilder.Add(config.Key, project);
            }

            ConfiguredProjects = projectBuilder.ToImmutable();
        }
    }
}
