// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class ProcessRunner : IProcessRunner
    {
        private static readonly string[] EnvVarsToWithold = new string[] { "MSBuildSDKsPath", "MSBuildExtensionsPath", "MSBUILD_EXE_PATH" };
        private readonly ILogger<ProcessRunner> _logger;

        public ProcessRunner(ILogger<ProcessRunner> logger)
        {
            _logger = logger;
        }

        public async Task<bool> RunProcessAsync(ProcessInfo args, CancellationToken token)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(args.Command, args.Arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            _logger.LogTrace("Starting process '{Command} {Arguments}'", args.Command, args.Arguments);

            foreach (var (name, value) in args.EnvironmentVariables)
            {
                process.StartInfo.EnvironmentVariables[name] = value;
            }

            foreach (var envVar in EnvVarsToWithold)
            {
                if (process.StartInfo.EnvironmentVariables.ContainsKey(envVar))
                {
                    process.StartInfo.EnvironmentVariables.Remove(envVar);
                }
            }

            var errorEncountered = false;

            process.OutputDataReceived += args.Quiet ? QuietOutputReceived : OutputReceived;
            process.ErrorDataReceived += args.Quiet ? QuietOutputReceived : OutputReceived;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(token).ConfigureAwait(false);

            if (process.ExitCode != args.SuccessCode)
            {
                if (args.Quiet)
                {
                    _logger.LogDebug("[{Tool}] Exited with non-success code: {ExitCode}", args.DisplayName, process.ExitCode);
                }
                else
                {
                    _logger.LogError("[{Tool}] Exited with non-success code: {ExitCode}", args.DisplayName, process.ExitCode);
                }

                return false;
            }

            if (errorEncountered)
            {
                _logger.LogError("[{Tool}] Exited with caller-defined errors", args.DisplayName);
                return false;
            }

            return true;

            void OutputReceived(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    CheckForErrors(e.Data);
                    _logger.LogInformation("[{Tool}] {Data}", args.DisplayName, e.Data);
                }
            }

            void QuietOutputReceived(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    CheckForErrors(e.Data);
                    _logger.LogDebug("[{Tool}] {Data}", args.DisplayName, e.Data);
                }
            }

            void CheckForErrors(string data)
            {
                if (args.IsErrorFilter(data))
                {
                    errorEncountered = true;
                }
            }
        }
    }
}
