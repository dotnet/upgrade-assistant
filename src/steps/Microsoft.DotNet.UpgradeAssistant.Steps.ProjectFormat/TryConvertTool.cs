// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        private const string TryConvertArgumentsFormat = "--no-backup --force-web-conversion --keep-current-tfms -p \"{0}\"";
        private static readonly string[] ErrorMessages = new[]
        {
            "This project has custom imports that are not accepted by try-convert"
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

            Path = Environment.ExpandEnvironmentVariables(tryConvertOptionsAccessor.Value.TryConvertPath);
        }

        public string Path { get; }

        public bool IsAvailable => File.Exists(Path);

        public string GetCommandLine(IProject project)
            => Invariant($"{Path} {GetArguments(project)}");

        public Task<bool> RunAsync(IMigrationContext context, IProject project, CancellationToken token)
            => _runner.RunProcessAsync(new ProcessInfo
            {
                Command = Path,
                Arguments = GetArguments(project),
                EnvironmentVariables = context.GlobalProperties,
                IsErrorFilter = data => ErrorMessages.Any(data.Contains),
            }, token);

        private string GetArguments(IProject project) => string.Format(CultureInfo.InvariantCulture, TryConvertArgumentsFormat, project.FilePath);
    }
}
