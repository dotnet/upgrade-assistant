// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Evaluation;

namespace MSBuild.Abstractions
{
    public class MSBuildProjectProperty : IProjectProperty
    {
        private readonly ProjectProperty _property;

        public MSBuildProjectProperty(ProjectProperty property)
        {
            _property = property;
        }

        public string Name => _property.Name;

        public string EvaluatedValue => _property.EvaluatedValue;

        public string UnevaluatedValue => _property.UnevaluatedValue;

        public bool IsDefinedInProject => !_property.IsImported &&
                                          !_property.IsEnvironmentProperty &&
                                          !_property.IsGlobalProperty &&
                                          !_property.IsReservedProperty;
    }
}
