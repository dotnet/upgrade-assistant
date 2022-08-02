using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace MSBuild.Abstractions
{
    public class MSBuildConversionWorkspaceLoader
    {
        private readonly string _workspacePath;
        private readonly MSBuildConversionWorkspaceType _workspaceType;

        public MSBuildConversionWorkspaceLoader(string workspacePath, MSBuildConversionWorkspaceType workspaceType)
        {
            if (string.IsNullOrWhiteSpace(workspacePath))
            {
                throw new ArgumentException($"{workspacePath} cannot be null or empty.");
            }

            if (!File.Exists(workspacePath))
            {
                throw new FileNotFoundException(workspacePath);
            }

            _workspacePath = workspacePath;
            _workspaceType = workspaceType;
        }

        public MSBuildConversionWorkspace LoadWorkspace(string path, bool noBackup, string tfm, bool keepCurrentTFMs, bool forceWeb)
        {
            var projectPaths =
                _workspaceType switch
                {
                    MSBuildConversionWorkspaceType.Project => ImmutableArray.Create(path),
                    MSBuildConversionWorkspaceType.Solution =>
                        SolutionFile.Parse(_workspacePath).ProjectsInOrder
                            .Where(IsSupportedSolutionItemType)
                            .Select(p => p.AbsolutePath).ToImmutableArray(),
                    _ => throw new InvalidOperationException("Somehow, an enum that isn't possible was passed in here.")
                };

            return new MSBuildConversionWorkspace(projectPaths, noBackup, tfm, keepCurrentTFMs, forceWeb);

            static bool IsSupportedSolutionItemType(ProjectInSolution project)
            {
                if (project.ProjectType != SolutionProjectType.KnownToBeMSBuildFormat &&
                    project.ProjectType != SolutionProjectType.SolutionFolder)
                {
                    Console.WriteLine($"{project.AbsolutePath} is not a supported solution item and will be skipped.");
                }

                return project.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat;
            }
        }

        public IProjectRootElement GetRootElementFromProjectFile(string projectFilePath = "")
        {
            var path = Path.GetFullPath(projectFilePath);

            if (!File.Exists(path))
            {
                throw new ArgumentException($"The project file '{projectFilePath}' does not exist or is inaccessible.");
            }

            using var collection = new ProjectCollection();

            return new MSBuildProjectRootElement(ProjectRootElement.Open(path, collection, preserveFormatting: true));
        }
    }
}
