﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

using static System.FormattableString;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat
{
    public class TryConvertTool : ITryConvertTool
    {
        private const string StorePath = ".store/try-convert";
        private const string TryConvertArgumentsFormat = "--no-backup --force-web-conversion --keep-current-tfms -p \"{0}\"";
        private static readonly string[] ErrorMessages = new[]
        {
            "This project has custom imports that are not accepted by try-convert",
            "is an unsupported project type. Not all project type guids are supported."
        };

        private readonly IProcessRunner _runner;

        public TryConvertTool(
            IProcessRunner runner,
            IOptions<TryConvertProjectConverterStepOptions> tryConvertOptionsAccessor)
        {
            _runner = runner;

            if (tryConvertOptionsAccessor is null)
            {
                throw new ArgumentNullException(nameof(tryConvertOptionsAccessor));
            }

            Path = tryConvertOptionsAccessor.Value.TryConvertPath;
        }

        public string Path { get; }

        public bool IsAvailable => File.Exists(Path);

        public string GetCommandLine(IProject project)
            => Invariant($"{Path} {GetArguments(project.Required())}");

        public Task<bool> RunAsync(IUpgradeContext context, IProject project, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _runner.RunProcessAsync(new ProcessInfo
            {
                Command = Path,
                Arguments = GetArguments(project.Required()),
                EnvironmentVariables = context.GlobalProperties,
                IsErrorFilter = data => ErrorMessages.Any(data.Contains),
            }, token);
        }

        public string? TryGetVersion()
        {
            // The host that runs .NET CLI tools (like try-convert) stores
            // its version as a product version attribute.
            var version = FileVersionInfo.GetVersionInfo(Path);
            if (version?.ProductVersion is string productVersion)
            {
                var commitIndex = productVersion.IndexOf('+');
                return commitIndex >= 0
                    ? productVersion.Substring(0, commitIndex)
                    : productVersion;
            }

            // Local .NET CLI tools (like try-convert) typically have their implementations in a version-specific
            // folder inside the hidden .store path next to the host. In case the version being stored in the
            // tool's product version attribute ever changes, this could be used as a backup means of getting
            // try-convert's version.
            var storeDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), StorePath);
            if (Directory.Exists(storeDir))
            {
                var versionDirs = Directory.GetDirectories(storeDir);
                if (versionDirs.Length == 1)
                {
                    return System.IO.Path.GetFileName(versionDirs[0]);
                }
            }

            return null;
        }

        private static string GetArguments(IProject project) => string.Format(CultureInfo.InvariantCulture, TryConvertArgumentsFormat, project.Required().FilePath);
    }
}
