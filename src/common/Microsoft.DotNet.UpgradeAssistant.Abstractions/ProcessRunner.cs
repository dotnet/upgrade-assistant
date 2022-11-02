// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(args.Command, args.Arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
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
            var tcs = new TaskCompletionSource<bool>();

            using var registration = token.Register(() => tcs.SetCanceled());

            process.Exited += (_, __) => tcs.SetResult(true);
            process.OutputDataReceived += OutputReceived;
            process.ErrorDataReceived += ErrorReceived;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await tcs.Task.ConfigureAwait(false);

            // Async output may still be pending (see Process.HasExited)
            process.WaitForExit();

            if (process.ExitCode != args.SuccessCode)
            {
                const string Message = "[{Tool}] Error: Exited with non-success code: {ExitCode}";
                _logger.Log(args.GetMessageLogLevel(true, Message), Message, args.DisplayName, process.ExitCode);

                return false;
            }

            if (errorEncountered)
            {
                _logger.LogError("[{Tool}] Exited with caller-defined errors", args.DisplayName);
                return false;
            }

            return true;

            void OutputReceived(object sender, DataReceivedEventArgs e) => ProcessMessage(false, e.Data);

            void ErrorReceived(object sender, DataReceivedEventArgs e) => ProcessMessage(true, e.Data);

            void ProcessMessage(bool isStdErr, string message)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    CheckForErrors(message);
                    _logger.Log(args.GetMessageLogLevel(isStdErr, message), "[{Tool}] {Data}", args.DisplayName, message);
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
