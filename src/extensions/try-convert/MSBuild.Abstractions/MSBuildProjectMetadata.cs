// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Evaluation;

namespace MSBuild.Abstractions
{
    public class MSBuildProjectMetadata : IProjectMetadata
    {
        private readonly ProjectMetadata _projectMetadata;

        public MSBuildProjectMetadata(ProjectMetadata projectMetadata)
        {
            _projectMetadata = projectMetadata;
        }

        public string Name => _projectMetadata.Name;

        public string UnevaluatedValue => _projectMetadata.UnevaluatedValue;

        public string EvaluatedValue => _projectMetadata.EvaluatedValue;

        public bool Equals(IProjectMetadata? other)
        {
            return other != null &&
                   _projectMetadata.Name.Equals(other.Name) &&
                   _projectMetadata.UnevaluatedValue.Equals(other.UnevaluatedValue) &&
                   _projectMetadata.EvaluatedValue.Equals(other.EvaluatedValue);
        }
    }
}
