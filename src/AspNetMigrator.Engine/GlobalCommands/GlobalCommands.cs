using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine.GlobalCommands
{
    public static class GlobalCommands
    {
        public static List<MigrationCommand> GetCommands(Func<List<UserMessage>, Task> sendMessageToUser, Action exitAction = null)
        {
            if (exitAction is null)
            {
                exitAction = DefaultExitAction;
            }

            var listOfCommands = new List<MigrationCommand>();
            listOfCommands.Add(new SkipNextCommand());
            listOfCommands.Add(new ConfigureLoggingCommand());
            listOfCommands.Add(new SeeMoreDetailsCommand(sendMessageToUser));
            listOfCommands.Add(new ExitCommand(exitAction));

            return listOfCommands;
        }

        private static void DefaultExitAction()
        {
            Environment.Exit(0);
        }
    }
}
