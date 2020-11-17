using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine.GlobalCommands
{
    public class SeeMoreDetailsCommand : MigrationCommand
    {
        private readonly Func<List<UserMessage>, Task> _sendMessageToUserAsync;

        // todo - support localization
        public SeeMoreDetailsCommand(Func<List<UserMessage>, Task> sendMessageToUserAsync)
        {
            // todo - something that handles a list of UserMessages
            _sendMessageToUserAsync = sendMessageToUserAsync ?? throw new ArgumentNullException(nameof(sendMessageToUserAsync));
        }

        // todo - support localization
        public override string CommandText => "See more step details";

        public override async Task<bool> ExecuteAsync(Migrator migrator)
        {
            if (migrator is null)
            {
                throw new ArgumentNullException(nameof(migrator));
            }

            var listOfMessages = new List<UserMessage>();

            // try
            // {
            if (migrator.NextStep is null)
            {
                listOfMessages.Add(new UserMessage
                {
                    Category = UserMessageCategory.Warning,

                    // todo - support localization
                    Message = "No current step to get details for"
                });
            }
            else
            {
                listOfMessages.Add(new UserMessage
                {
                    Category = UserMessageCategory.None,
                    Message = string.Empty
                });

                listOfMessages.Add(new UserMessage
                {
                    Category = UserMessageCategory.Info,

                    // todo - support localization
                    Message = "Current step details"
                });

                listOfMessages.Add(new UserMessage
                {
                    Category = UserMessageCategory.None,
                    Message = migrator.NextStep.Description,
                });

                listOfMessages.Add(new UserMessage
                {
                    Category = UserMessageCategory.None,
                    Message = migrator.NextStep.StatusDetails,
                });
            }

            await _sendMessageToUserAsync(listOfMessages).ConfigureAwait(false);
            return true;

            // }
            // catch (Exception ex)
            // {
            //    // todo - add logger
            //    return false;
            // }
        }
    }
}
