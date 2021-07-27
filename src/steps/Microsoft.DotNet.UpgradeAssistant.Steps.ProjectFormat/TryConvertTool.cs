// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static System.FormattableString;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat
{
    public class TryConvertTool : ITryConvertTool
    {
        private const string DotNetCli = "dotnet";

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
        private readonly IEnumerable<string> _arguments;

        public TryConvertTool(
            IProcessRunner runner,
            IOptions<ICollection<TryConvertOptions>> allOptions,
            IOptions<TryConvertOptions> options,
            MSBuildPathLocator locator)
        {
            if (allOptions is null)
            {
                throw new ArgumentNullException(nameof(allOptions));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            _arguments = allOptions.Value.SelectMany(o => o.Arguments);

            Path = options.Value.ToolPath;
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
                Command = DotNetCli,
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

        private string GetArguments(IProject project)
        {
            var sb = new CommandLineBuilder();

            sb.AddQuotedString(Path);
            sb.AddArgument("-m", GetMSBuildPath());
            sb.AddArgument("-p", project.FileInfo.FullName);

            foreach (var argument in _arguments)
            {
                sb.AddArgument(argument);
            }

            return sb.ToString();
        }

        private class CommandLineBuilder
        {
            private const char Space = ' ';
            private const char Quote = '"';

            private readonly StringBuilder _sb = new();

            private bool _hasAtLeastOne;

            public void AddArgument(string arg, string? value = null)
            {
                AddSpaceIfNeeded();

                _sb.Append(arg);

                if (value is not null)
                {
                    AddQuotedString(value);
                }
            }

            public void AddQuotedString(string value)
            {
                AddSpaceIfNeeded();
                _sb.Append(Quote);
                _sb.Append(value);
                _sb.Append(Quote);
            }

            private void AddSpaceIfNeeded()
            {
                if (_hasAtLeastOne)
                {
                    _sb.Append(Space);
                }
                else
                {
                    _hasAtLeastOne = true;
                }
            }

            public override string ToString() => _sb.ToString();
        }
    }
}
