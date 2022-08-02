using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Build.Construction;

namespace MSBuild.Abstractions
{
    public interface IProjectRootElement
    {
        string? ToolsVersion { get; set; }
        string Sdk { get; set; }
        string? DefaultTargets { get; set; }
        string DirectoryPath { get; }
        string FullPath { get; }
        string RawXml { get; }
        XDocument Xml { get; }
        ICollection<ProjectImportElement> Imports { get; }
        ICollection<ProjectImportGroupElement> ImportGroups { get; }
        ICollection<ProjectPropertyGroupElement> PropertyGroups { get; }
        ICollection<ProjectItemGroupElement> ItemGroups { get; }
        ICollection<ProjectTargetElement> Targets { get; }
        ProjectExtensionsElement? ProjectExtensions { get; }

        ProjectPropertyElement CreatePropertyElement(string propertyName);
        ProjectPropertyGroupElement AddPropertyGroup();
        ProjectItemGroupElement AddItemGroup();

        void Save(string path);
        void RemoveChild(ProjectElement child);
        void Reload(bool throwIfUnsavedChanges = true, bool? preserveFormatting = null);
    }

    public class MSBuildProjectRootElement : IProjectRootElement
    {
        private const string ProjectExtensionsElementName = "ProjectExtensions";

        private readonly ProjectRootElement _rootElement;

        public MSBuildProjectRootElement(ProjectRootElement rootElement)
        {
            _rootElement = rootElement;
        }

        public string? ToolsVersion { get => _rootElement.ToolsVersion; set => _rootElement.ToolsVersion = value; }
        public string Sdk { get => _rootElement.Sdk; set => _rootElement.Sdk = value; }
        public string? DefaultTargets { get => _rootElement.DefaultTargets; set => _rootElement.DefaultTargets = value; }
        public string DirectoryPath => _rootElement.DirectoryPath;
        public string FullPath => _rootElement.FullPath;
        public string RawXml => _rootElement.RawXml;
        public XDocument Xml => XDocument.Parse(_rootElement.RawXml);
        public ICollection<ProjectImportElement> Imports => _rootElement.Imports;
        public ICollection<ProjectImportGroupElement> ImportGroups => _rootElement.ImportGroups;
        public ICollection<ProjectPropertyGroupElement> PropertyGroups => _rootElement.PropertyGroups;
        public ICollection<ProjectItemGroupElement> ItemGroups => _rootElement.ItemGroups;
        public ICollection<ProjectTargetElement> Targets => _rootElement.Targets;
        public ProjectExtensionsElement? ProjectExtensions => _rootElement.Children.FirstOrDefault(e => e.ElementName.Equals(ProjectExtensionsElementName, StringComparison.OrdinalIgnoreCase)) as ProjectExtensionsElement;

        public ProjectItemGroupElement AddItemGroup() => _rootElement.AddItemGroup();

        public ProjectPropertyGroupElement AddPropertyGroup() => _rootElement.AddPropertyGroup();
        public ProjectPropertyElement CreatePropertyElement(string name) => _rootElement.CreatePropertyElement(name);

        public void Reload(bool throwIfUnsavedChanges = true, bool? preserveFormatting = null) => _rootElement.Reload(throwIfUnsavedChanges, preserveFormatting);
        public void RemoveChild(ProjectElement child) => _rootElement.RemoveChild(child);
        public void Save(string path) => _rootElement.Save(path);
    }
}
