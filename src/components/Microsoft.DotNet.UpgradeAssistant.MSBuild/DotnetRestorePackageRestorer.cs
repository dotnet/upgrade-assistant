// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using static System.FormattableString;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class DotnetRestorePackageRestorer : IPackageRestorer
    {
        private static readonly string[] MessagesToDisplay = new[]
        {
            "Consider re-running the command with --interactive",
            "ATTENTION: User interaction required",
            "*****************************",
            "To sign in, use a web browser",
            "FromWorkload",
            "workload",
        };

        private readonly IUserInput _userInput;
        private readonly ILogger<DotnetRestorePackageRestorer> _logger;
        private readonly IProcessRunner _runner;

        public DotnetRestorePackageRestorer(IUserInput userInput, ILogger<DotnetRestorePackageRestorer> logger, IProcessRunner runner)
        {
            _userInput = userInput ?? throw new ArgumentNullException(nameof(userInput));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public async Task<bool> RestorePackagesAsync(IUpgradeContext context, IProject project, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            _logger.LogDebug("Restoring packages for {ProjectPath} with dotnet restore", project.FileInfo.FullName);
            var result = await RunRestoreAsync(context, project.FileInfo.FullName, token).ConfigureAwait(false);

            // Reload the project because, by design, NuGet properties (like NuGetPackageRoot)
            // aren't available in a project until after restore is run the first time.
            // https://github.com/NuGet/Home/issues/9150
            await context.ReloadWorkspaceAsync(token).ConfigureAwait(false);

            return result;
        }

        // Run `dotnet restore` using quiet mode since some warnings and errors are
        // expected. As long as a lock file is produced (which is checked elsewhere),
        // the tool was successful enough.
        public Task<bool> RunRestoreAsync(IUpgradeContext context, string path, CancellationToken token) =>
            _runner.RunProcessAsync(new ProcessInfo
            {
                Command = "dotnet",
                Arguments = _userInput.IsInteractive ? Invariant($"restore --interactive \"{path}\"") : Invariant($"restore \"{path}\""),
                EnvironmentVariables = context.GlobalProperties,
                Name = "dotnet-restore",

                // dotnet-restore does not need to receive user input to authenticate, so the only thing
                // necessary to enable interactive auth is to make sure that necessary messages are displayed to users.
                GetMessageLogLevel = (_, message) => MessagesToDisplay.Any(m => message.Contains(m, StringComparison.Ordinal)) ? LogLevel.Information : LogLevel.Debug,
            }, token);
    }
}
