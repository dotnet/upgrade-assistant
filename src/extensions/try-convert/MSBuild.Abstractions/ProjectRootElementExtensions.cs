﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Construction;

namespace MSBuild.Abstractions
{
    public static class ProjectRootElementExtensions
    {
        /// <summary>
        /// Gets the OutputType node in a project. There will only reasonably be one.
        /// </summary>
        public static ProjectPropertyElement? GetOutputTypeNode(this IProjectRootElement root) =>
            root.PropertyGroups.SelectMany(pg => pg.Properties.Where(ProjectPropertyHelpers.IsOutputTypeNode)).FirstOrDefault();
    }
}
