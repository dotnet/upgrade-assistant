﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class ComponentIdentifier : IComponentIdentifier
    {
        public ProjectComponents GetComponents(IProject project)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var file = project.GetFile();

            // SDK-style projects can target .NET Framework and use GAC-referenced app models,
            // so old project components are checked regardless of SDK status
            var components = GetOldProjectComponents(project, file);
            if (file.IsSdk)
            {
                components |= GetSDKProjectComponents(project, file);
            }

            if (project.TransitivePackageReferences.Any(f => MSBuildConstants.WinRTPackages.Contains(f.Name, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.WinRT;
            }

            return components;
        }

        // Gets project components based on SDK, properties, and FrameworkReferences
        private static ProjectComponents GetSDKProjectComponents(IProject project, IProjectFile file)
        {
            var components = ProjectComponents.None;
            if (file.Sdk.Equals(MSBuildConstants.WebSdk, StringComparison.OrdinalIgnoreCase))
            {
                components |= ProjectComponents.AspNetCore;
            }

            if (file.GetPropertyValue("UseWPF").Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                components |= ProjectComponents.Wpf;
                components |= ProjectComponents.WindowsDesktop;
            }

            if (file.GetPropertyValue("UseWindowsForms").Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                components |= ProjectComponents.WinForms;
                components |= ProjectComponents.WindowsDesktop;
            }

            if (file.Sdk.Equals(MSBuildConstants.DesktopSdk, StringComparison.OrdinalIgnoreCase))
            {
                components |= ProjectComponents.WindowsDesktop;
            }

            var frameworkReferenceNames = project.FrameworkReferences.Select(r => r.Name);
            if (frameworkReferenceNames.Any(f => MSBuildConstants.WebFrameworkReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.AspNetCore;
            }

            if (frameworkReferenceNames.Any(f => MSBuildConstants.DesktopFrameworkReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.WindowsDesktop;
            }

            if (frameworkReferenceNames.Any(f => MSBuildConstants.WinFormsReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.WinForms;
            }

            if (frameworkReferenceNames.Any(f => MSBuildConstants.WpfReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.Wpf;
            }

            return components;
        }

        // Gets project components based on imports and References
        private static ProjectComponents GetOldProjectComponents(IProject project, IProjectFile file)
        {
            var components = ProjectComponents.None;

            // Check imports and references
            var references = project.References.Select(r => r.Name);

            if (file.Imports.Contains(MSBuildConstants.WebApplicationTargets, StringComparer.OrdinalIgnoreCase) ||
                references.Any(r => MSBuildConstants.WebReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.AspNet;
            }

            if (references.Any(r => MSBuildConstants.WinFormsReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.WindowsDesktop;
                components |= ProjectComponents.WinForms;
            }

            if (references.Any(r => MSBuildConstants.WpfReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                components |= ProjectComponents.WindowsDesktop;
                components |= ProjectComponents.Wpf;
            }

            return components;
        }
    }
}
