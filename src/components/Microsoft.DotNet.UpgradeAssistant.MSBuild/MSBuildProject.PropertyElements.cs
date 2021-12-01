// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal partial class MSBuildProject : IProjectPropertyElements
    {
        IProjectPropertyElements IProject.GetProjectPropertyElements() => this;

        // IEnumerable instead of IDictionary because some project properties can be duplicated. This is a raw list that can be filtered later as needed.
        public IEnumerable<ProjectPropertyValue> ProjectProperties => Project.Xml.Properties.Select(p => new ProjectPropertyValue(p.Name, p.Value));

        public IEnumerable<string> GetProjectPropertyValue(string propertyName) => Project.Xml.Properties.Where(x => x.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);

        public void RemoveProjectProperty(string propertyName)
        {
            var propertiesToDelete = Project.Xml.Properties.Where(x => x.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
            foreach (var property in propertiesToDelete)
            {
                _logger.LogInformation("Removing Project Property: {ProjectProperty} Value : {PropertyValue}", property.Name, property.Value);
                ProjectRoot.RemoveProjectProperty(property);
            }
        }
    }
}
