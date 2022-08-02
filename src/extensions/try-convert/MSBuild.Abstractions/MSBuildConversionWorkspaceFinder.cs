// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

// Original License:
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// See https://github.com/aspnet/DotNetTools/blob/261b27b70027871143540af10a5cba57ce07ff97/src/dotnet-watch/Internal/MsBuildProjectFinder.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MSBuild.Abstractions
{
    public class MSBuildConversionWorkspaceFinder
    {
        // Used to exclude dnx projects
        private const string DnxProjectExtension = ".xproj";

        /// <summary>
        /// Finds a compatible MSBuild project or solution.
        /// <param name="searchDirectory">The base directory to search</param>
        /// <param name="workspacePath">A specific project or solution file to find</param>
        /// </summary>
        public static (bool isSolution, string workspacePath) FindWorkspace(string searchDirectory, string? workspacePath = null)
        {
            if (!string.IsNullOrEmpty(workspacePath))
            {
                if (!Path.IsPathRooted(workspacePath))
                {
                    workspacePath = Path.GetFullPath(workspacePath, searchDirectory);
                }

                return Directory.Exists(workspacePath)
                    ? FindWorkspace(workspacePath)
                    : FindFile(workspacePath);
            }

            var foundSolution = FindMatchingFile(searchDirectory, FindSolutionFiles, "Multiple MSBuild solution files were found. Please specify which to use with '-w'.");
            var foundProject = FindMatchingFile(searchDirectory, FindProjectFiles, "Multiple MSBuild project files were found. Please specify which to use with '-w'.");

            if (!string.IsNullOrEmpty(foundSolution) && !string.IsNullOrEmpty(foundProject))
            {
                throw new FileNotFoundException(string.Format("Both an MSBuild project file and a solution file were found in '{0}'. Please specify which to use with '-w'.", searchDirectory));
            }

            if (!string.IsNullOrEmpty(foundSolution) && string.IsNullOrEmpty(foundProject))
            {
                return (true, foundSolution);
            }
            else if (!string.IsNullOrEmpty(foundProject) && string.IsNullOrEmpty(foundSolution))
            {
                return (false, foundProject);
            }

            throw new FileNotFoundException(string.Format("Could not find any an MSBuild project file or solution file in '{0}'. Please specify which to use with '-w'.", searchDirectory));
        }

        private static (bool isSolution, string workspacePath) FindFile(string workspacePath)
        {
            var workspaceExtension = Path.GetExtension(workspacePath);
            var isSolution = workspaceExtension.Equals(".sln", StringComparison.OrdinalIgnoreCase);
            var isProject = !isSolution
                && workspaceExtension.EndsWith("proj", StringComparison.OrdinalIgnoreCase)
                && !workspaceExtension.Equals(DnxProjectExtension, StringComparison.OrdinalIgnoreCase);

            if (!isSolution && !isProject)
            {
                throw new FileNotFoundException(string.Format("The file '{0} 'does not appear to be a valid project or solution file.", Path.GetFileName(workspacePath)));
            }

            if (!File.Exists(workspacePath))
            {
                var message = isSolution
                    ? "The solution file '{0}' does not exist."
                    : "The project file '{0}' does not exist.";
                throw new FileNotFoundException(string.Format(message, workspacePath));
            }

            return (isSolution, workspacePath);
        }

        private static IEnumerable<string> FindSolutionFiles(string basePath)
            => Directory.EnumerateFileSystemEntries(basePath, "*.sln", SearchOption.TopDirectoryOnly);

        private static IEnumerable<string> FindProjectFiles(string basePath)
            => Directory.EnumerateFileSystemEntries(basePath, "*.*proj", SearchOption.TopDirectoryOnly)
                        .Where(f => !DnxProjectExtension.Equals(Path.GetExtension(f), StringComparison.OrdinalIgnoreCase));

        private static string? FindMatchingFile(string searchBase, Func<string, IEnumerable<string>> fileSelector, string multipleFilesFoundError)
        {
            if (!Directory.Exists(searchBase))
            {
                return null;
            }

            var files = fileSelector(searchBase).ToList();
            if (files.Count > 1)
            {
                throw new FileNotFoundException(string.Format(multipleFilesFoundError, searchBase));
            }

            return files.Count == 1
                ? files[0]
                : null;
        }
    }
}
