using System;
using AspNetMigrator.Engine.GlobalCommands;

namespace AspNetMigrator.ConsoleApp
{
    public static class CommandResultHandlerFactory
    {
        public static CommandResultHandler GetCommandResult(Type command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (typeof(ApplyNextCommand) == command.GetType())
            {
                return new ApplyNextCommandResultHandler();
            }
            else if (typeof(ConfigureLoggingCommand) == command.GetType())
            {
                return new ConfigureLoggingCommandResultHandler();
            }
            else if (typeof(SeeMoreDetailsCommand) == command.GetType())
            {
                return new SeeMoreDetailsCommandResultHandler();
            }
            else if (typeof(SkipNextCommand) == command.GetType())
            {
                return new SkipNextCommandResultHandler();
            }
            else if (typeof(SetBackupPathCommandResultHandler) == command.GetType())
            {
                return new SetBackupPathCommandResultHandler();
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(command));
            }
        }
    }
}
