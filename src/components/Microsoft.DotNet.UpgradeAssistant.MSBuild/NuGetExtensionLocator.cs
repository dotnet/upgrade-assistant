// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from https://github.com/NuGet/NuGet.Client/blob/17c4f841ff61d27fe6b57cf45ceef16037062635/src/NuGet.Clients/NuGet.CommandLine/ExtensionLocator.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Configuration;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    /// <summary>
    /// Provides a common facility for locating NuGet extensions.
    /// </summary>
    public class NuGetExtensionLocator : IExtensionLocator
    {
        private const string NuGetRelativePath = @"Common7\IDE\CommonExtensions\Microsoft\NuGet";
        private const string CredentialProviderPattern = "CredentialProvider*.exe";
        private const string ExtensionsEnvar = "NUGET_EXTENSIONS_PATH";
        private const string CredentialProvidersEnvar = "NUGET_CREDENTIALPROVIDERS_PATH";

        private static readonly string ExtensionsDirectoryRoot =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
                "NuGet",
                "Commands");

        private static readonly string CredentialProvidersDirectoryRoot =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
                "NuGet",
                "CredentialProviders");

        private readonly string? _vsPath;
        private readonly ILogger<NuGetExtensionLocator> _logger;

        public NuGetExtensionLocator(IVisualStudioFinder vsFinder, ILogger<NuGetExtensionLocator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _vsPath = vsFinder?.GetLatestVisualStudioPath();
        }

        /// <summary>
        /// Find paths to all extensions.
        /// </summary>
        /// <returns>
        /// An enumerable of strings representing the extension paths.
        /// </returns>
        public IEnumerable<string> FindExtensions()
        {
            var customPaths = ReadPathsFromEnvar(ExtensionsEnvar);
            return FindAll(
                ExtensionsDirectoryRoot,
                customPaths,
                "*.dll",
                "*Extensions.dll");
        }

        /// <summary>
        /// Find paths to all credential providers.
        /// </summary>
        /// <returns>
        /// An enumerable of strings representing the credential provider paths.
        /// </returns>
        public IEnumerable<string> FindCredentialProviders()
        {
            var customPaths = ReadPathsFromEnvar(CredentialProvidersEnvar);
            return FindAll(
                CredentialProvidersDirectoryRoot,
                customPaths,
                CredentialProviderPattern,
                CredentialProviderPattern);
        }

        /// <summary>
        /// Helper method to locate extensions and credential providers.
        /// The following search locations will be checked in this order:
        /// 1) all directories under a path set by environment variable
        /// 2) all directories under a global path, e.g. %localappdata%\nuget\commands
        /// 3) the directory nuget.exe is located in.
        /// </summary>
        /// <param name="globalRootDirectory">The global directory to search.  Will
        /// also check subdirectories.</param>
        /// <param name="customPaths">User-defined search paths.
        /// Will also check subdirectories.</param>
        /// <param name="assemblyPattern">The filename pattern to search for.</param>
        /// <param name="nugetDirectoryAssemblyPattern">The filename pattern to search for
        /// when looking in the nuget.exe directory. This is more restrictive so we do not
        /// accidentally pick up NuGet dlls.</param>
        /// <returns>An IEnumerable of paths to files matching the pattern in all searched
        /// directories.</returns>
        private IEnumerable<string> FindAll(
            string globalRootDirectory,
            IEnumerable<string> customPaths,
            string assemblyPattern,
            string nugetDirectoryAssemblyPattern)
        {
            var directories = new List<string>();

            // Add all directories from the environment variable if available.
            directories.AddRange(customPaths);
            _logger.LogDebug("Searching for NuGet extensions in paths: {Paths}", string.Join(',', customPaths));

            // add the global root
            directories.Add(globalRootDirectory);
            _logger.LogDebug("Searching for NuGet extensions in NuGet global root: {Path}", globalRootDirectory);

            var paths = new List<string>();
            foreach (var directory in directories.Where(Directory.Exists))
            {
                paths.AddRange(Directory.EnumerateFiles(directory, assemblyPattern, SearchOption.AllDirectories));
            }

            // Add the nuget.exe directory, but be more careful since it contains non-extension assemblies.
            // Ideally we want to look for all files. However, using MEF to identify imports results in assemblies
            // being loaded and locked by our App Domain which could be slow, might affect people's build systems
            // and among other things breaks our build.
            // Consequently, we'll use a convention - only binaries ending in the name Extensions would be loaded.
            if (_vsPath is not null)
            {
                var nugetDirectory = Path.Combine(_vsPath, NuGetRelativePath);
                if (nugetDirectory != null && Directory.Exists(nugetDirectory))
                {
                    paths.AddRange(Directory.EnumerateFiles(nugetDirectory, nugetDirectoryAssemblyPattern, SearchOption.AllDirectories));
                    _logger.LogDebug("Searching for NuGet extensions in VS install dir: {Path}", nugetDirectory);
                }
            }

            return paths;
        }

        private static IEnumerable<string> ReadPathsFromEnvar(string key)
        {
            var result = new List<string>();
            var paths = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(paths))
            {
                result.AddRange(
                    paths.Split(new[] { ';' },
                    StringSplitOptions.RemoveEmptyEntries));
            }

            return result;
        }
    }
}
