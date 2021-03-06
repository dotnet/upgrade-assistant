// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat
{
    public class TryConvertToolStartup : IUpgradeStartup
    {
        private readonly ILogger _logger;
        private readonly IProcessRunner _runner;
        private readonly ITryConvertTool _tryConvertTool;

        public TryConvertToolStartup(ILogger<TryConvertToolStartup> logger,
            IProcessRunner runner,
            ITryConvertTool tryConvertTool)
        {
            _logger = logger;
            _runner = runner;
            _tryConvertTool = tryConvertTool;
        }

        public Task<bool> StartupAsync(CancellationToken token)
        {
            TryConvertToolInstance(token);
            return Task.FromResult(true);
        }

        public bool TryConvertToolInstance(CancellationToken token)
        {
            if (!_tryConvertTool.IsAvailable)
            {
                _logger.LogInformation("Try-Convert not found. This tool depends on the Try-Convert CLI tool, installing now to the path : {path}", _tryConvertTool.Path);

                return _runner.RunProcessAsync(new ProcessInfo
                {
                    Command = "dotnet",
                    Arguments = " tool install -g try-convert"
                }, token).ConfigureAwait(true).GetAwaiter().GetResult();
            }

            return true;
        }
    }
}
