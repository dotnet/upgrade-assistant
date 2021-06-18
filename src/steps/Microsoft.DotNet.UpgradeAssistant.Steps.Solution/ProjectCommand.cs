// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    internal class ProjectCommand : UpgradeCommand
    {
        private readonly bool _isCompleted;
        private readonly bool _isValid;

        public static ProjectCommand Create(IProject project) => new(project, false, true);

        public ProjectCommand(IProject project, bool isCompleted, bool isValid)
        {
            _isCompleted = isCompleted;
            _isValid = isValid;
            IsEnabled = !isCompleted && isValid;
            Project = project;
        }

        public override string Id => "Project";

        // Use ANSI escape codes to colorize parts of the output (https://en.wikipedia.org/wiki/ANSI_escape_code)
        public override string CommandText
        {
            get
            {
                var prefix = (_isCompleted, _isValid) switch
                {
                    (true, _) => "\u001b[32m[Completed]\u001b[0m ",
                    (_, false) => "\u001b[31m[Invalid]\u001b[0m  ",
                    _ => string.Empty
                };

                return $"{prefix}{Project.GetRoslynProject().Name}";
            }
        }

        public IProject Project { get; }

        public override Task<bool> ExecuteAsync(IUpgradeContext context, CancellationToken token)
            => Task.FromResult(true);
    }
}
