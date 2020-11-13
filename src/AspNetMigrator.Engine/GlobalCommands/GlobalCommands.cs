using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine.GlobalCommands
{
    public static class GlobalCommands
    {
        public static List<MigrationCommand> GetCommands(Migrator migrator, Func<UserMessage, Task> sendMessageToUser, Action exitAction = null)
        {
            if (migrator is null)
            {
                throw new ArgumentNullException(nameof(migrator));
            }

            if (exitAction is null)
            {
                exitAction = DefaultExitAction;
            }

            var listOfCommands = new List<MigrationCommand>();
            listOfCommands.Add(new ApplyNextCommand(migrator));
            listOfCommands.Add(new SkipNextCommand(migrator));
            listOfCommands.Add(new ConfigureLoggingCommand());
            listOfCommands.Add(new SeeMoreDetailsCommand(migrator, sendMessageToUser));
            listOfCommands.Add(new ExitCommand(exitAction));
            return listOfCommands;
        }

        private static void DefaultExitAction()
        {
            Environment.Exit(0);
        }
    }
}
