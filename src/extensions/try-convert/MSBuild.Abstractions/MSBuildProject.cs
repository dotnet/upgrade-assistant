// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Evaluation;

namespace MSBuild.Abstractions
{
    public class MSBuildProject : IProject
    {
        private readonly Project _project;

        public MSBuildProject(Project project) => _project = project ?? throw new ArgumentNullException(nameof(project));

        public ICollection<IProjectProperty> Properties => _project.Properties.Select(p => new MSBuildProjectProperty(p)).ToArray();

        public ICollection<IProjectItem> Items => _project.Items.Select(i => new MSBuildProjectItem(i)).ToArray();

        public IProjectProperty? GetProperty(string name) => _project.GetProperty(name) is { } ? new MSBuildProjectProperty(_project.GetProperty(name)) : null;

        public string GetPropertyValue(string name) => _project.GetPropertyValue(name);
    }

    /// <summary>
    /// Interface used to Mock access to MSBuild's Project apis.
    /// </summary>
    public interface IProject
    {
        ICollection<IProjectProperty> Properties { get; }

        ICollection<IProjectItem> Items { get; }

        IProjectProperty? GetProperty(string name);

        string GetPropertyValue(string name);
    }

    public interface IProjectProperty
    {
        string Name { get; }

        string EvaluatedValue { get; }

        string UnevaluatedValue { get; }

        bool IsDefinedInProject { get; }
    }

    public interface IProjectItem
    {
        string ItemType { get; }

        string EvaluatedInclude { get; }

        IEnumerable<IProjectMetadata> DirectMetadata { get; }
    }

    public interface IProjectMetadata : IEquatable<IProjectMetadata>
    {
        string Name { get; }

        string UnevaluatedValue { get; }

        string EvaluatedValue { get; }
    }
}
