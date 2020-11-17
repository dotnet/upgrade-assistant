using System;
using System.Collections.Generic;

namespace AspNetMigrator.ConsoleApp
{
    public class CommandResultHandlerFactory
    {
        private readonly Dictionary<Type, ICommandResultHandler> _handlerMap;
        private readonly ICommandResultHandler _defaultHandler;

        public CommandResultHandlerFactory(IEnumerable<ICommandResultHandler> commandResultHandlers, ICommandResultHandler defaultCommandResultHandler)
        {
            if (commandResultHandlers is null)
            {
                throw new ArgumentNullException(nameof(commandResultHandlers));
            }

            _defaultHandler = defaultCommandResultHandler;
            _handlerMap = CreateHandlerMap(commandResultHandlers);
        }

        private static Dictionary<Type, ICommandResultHandler> CreateHandlerMap(IEnumerable<ICommandResultHandler> commandResultHandlers)
        {
            var map = new Dictionary<Type, ICommandResultHandler>();
            foreach (var handler in commandResultHandlers)
            {
                var commandType = handler.GetTypeOfCommand();
                if (map.ContainsKey(commandType))
                {
                    throw new InvalidOperationException($"The type {commandType.Name} has already been registered.");
                }
                else
                {
                    map.Add(commandType, handler);
                }
            }

            return map;
        }

        public ICommandResultHandler GetHandler(Type typeOfCommand)
        {
            if (_handlerMap.ContainsKey(typeOfCommand))
            {
                return _handlerMap[typeOfCommand];
            }

            return _defaultHandler;
        }
    }
}
