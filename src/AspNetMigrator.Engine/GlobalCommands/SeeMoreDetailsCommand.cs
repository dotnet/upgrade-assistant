using System;
using System.Text;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine.GlobalCommands
{
    public enum UserMessageCategory
    {
        None, // console default color
        Info, // cyan
        Warning // yellow
    }

    public class UserMessage
    {
        public string Message { get; set; }

        public UserMessageCategory Category { get; set; }
    }

    public class SeeMoreDetailsCommand : MigrationCommand
    {
        private readonly Func<UserMessage, Task> _sendMessageToUserAsync;
        private readonly Migrator _migrator;

        // todo - support localization
        public SeeMoreDetailsCommand(Migrator migrator, Func<UserMessage, Task> sendMessageToUserAsync)
        {
            // todo - something that handles a list of UserMessages
            _migrator = migrator ?? throw new ArgumentNullException(nameof(migrator));
            _sendMessageToUserAsync = sendMessageToUserAsync ?? throw new ArgumentNullException(nameof(sendMessageToUserAsync));
        }

        // todo - support localization
        public override string CommandText => "See more step details";

        public override async Task<bool> ExecuteAsync()
        {
            // try
            // {
                if (_migrator.NextStep is null)
                {
                    await _sendMessageToUserAsync(new UserMessage
                    {
                        Category = UserMessageCategory.Warning,
                        Message = "No current step to get details for"
                    }).ConfigureAwait(false);

                    return true;
                }
                else
                {
                    await _sendMessageToUserAsync(new UserMessage
                    {
                        Category = UserMessageCategory.None,
                        Message = string.Empty
                    }).ConfigureAwait(false);

                    await _sendMessageToUserAsync(new UserMessage
                    {
                        Category = UserMessageCategory.Info,
                        Message = "Current step details"
                    }).ConfigureAwait(false);

                    await _sendMessageToUserAsync(new UserMessage
                    {
                        Category = UserMessageCategory.None,
                        Message = _migrator.NextStep.Description,
                    }).ConfigureAwait(false);

                    await _sendMessageToUserAsync(new UserMessage
                    {
                        Category = UserMessageCategory.None,
                        Message = _migrator.NextStep.StatusDetails,
                    }).ConfigureAwait(false);

                    return true;
                }

            // }
            // catch (Exception ex)
            // {
            //    // todo - add logger
            //    return false;
            // }
        }

    }
}
