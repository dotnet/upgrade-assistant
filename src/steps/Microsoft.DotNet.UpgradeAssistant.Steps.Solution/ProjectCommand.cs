using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    internal class ProjectCommand : MigrationCommand
    {
        public static ProjectCommand Create(IProject project) => new(project, false);

        public ProjectCommand(IProject project, bool isCompleted)
        {
            IsEnabled = !isCompleted;

            Project = project;
        }

        // Use ANSI escape codes to colorize parts of the output (https://en.wikipedia.org/wiki/ANSI_escape_code)
        public override string CommandText => IsEnabled ? Project.GetRoslynProject().Name : $"\u001b[32m[Completed]\u001b[0m {Project.GetRoslynProject().Name}";

        public IProject Project { get; }

        public override Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
            => Task.FromResult(true);
    }
}
