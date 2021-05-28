// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static System.FormattableString;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat
{
    public class TryConvertTool : ITryConvertTool
    {
        private const string TryConvertArgumentsFormat = "--no-backup -m \"{0}\" --force-web-conversion --keep-current-tfms -p \"{1}\"";
        private static readonly string[] ErrorMessages = new[]
        {
            "This project has custom imports that are not accepted by try-convert",
            "is an unsupported project type. Not all project type guids are supported.",
            "has invalid Project Type Guids for test projects and is not supported.",
            "is a coded UI test. Coded UI tests are deprecated and not convertable to .NET Core.",
            "is an unsupported MSTest test type. Only Unit Tests are supported.",
            "does not have a supported OutputType."
        };

        private readonly IProcessRunner _runner;
        private readonly MSBuildPathLocator _locator;

        public TryConvertTool(
            IProcessRunner runner,
            IOptions<TryConvertProjectConverterStepOptions> tryConvertOptionsAccessor,
            MSBuildPathLocator locator)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));

            if (tryConvertOptionsAccessor is null)
            {
                throw new ArgumentNullException(nameof(tryConvertOptionsAccessor));
            }

            Path = tryConvertOptionsAccessor.Value.TryConvertPath;
            Version = GetVersion();
        }

        public string Path { get; }

        public string? Version { get; }

        public bool IsAvailable => File.Exists(Path);

        public string GetCommandLine(IProject project)
            => Invariant($"{Path} {GetArguments(project.Required())}");

        private string? GetMSBuildPath()
        {
            return _locator.MSBuildPath?.Replace("\\", "\\\\");
        }

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
                GetMessageLogLevel = (isStdErr, msg) => (isStdErr || ErrorMessages.Any(msg.Contains)) ? LogLevel.Error : LogLevel.Information
            }, token);
        }

        private string? GetVersion()
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

            return null;
        }

        private string GetArguments(IProject project) => string.Format(CultureInfo.InvariantCulture, TryConvertArgumentsFormat, GetMSBuildPath(), project.Required().FileInfo);
    }
}
