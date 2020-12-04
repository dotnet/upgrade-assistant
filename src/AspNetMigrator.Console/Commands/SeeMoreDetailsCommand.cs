using System;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.ConsoleApp.Commands
{
    public class SeeMoreDetailsCommand : MigrationCommand
    {
        private readonly MigrationStep _step;
        private readonly Func<UserMessage, Task> _sendMessageToUserAsync;

        public SeeMoreDetailsCommand(MigrationStep step, Func<UserMessage, Task> sendMessageToUserAsync)
        {
            _step = step ?? throw new ArgumentNullException(nameof(step));
            _sendMessageToUserAsync = sendMessageToUserAsync ?? throw new ArgumentNullException(nameof(sendMessageToUserAsync));
        }

        // todo - support localization
        public override string CommandText => "See more step details";

        public override async Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
        {
            // TODO : In the future, it might be preferable to have a 'step status' object that we pass so that the caller
            //        can have more control over how title, description, and status details are formatted.
            await _sendMessageToUserAsync(new UserMessage
            {
                Severity = MessageSeverity.Info,
                Message = $"{_step.Title}\n{_step.Description}\nStatus: {_step.StatusDetails}"
            }).ConfigureAwait(false);
            return true;
        }
    }
}
