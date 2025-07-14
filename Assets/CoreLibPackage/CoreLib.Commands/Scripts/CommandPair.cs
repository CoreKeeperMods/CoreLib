using CoreLib.Commands.Communication;
using CoreLib.Commands.CoreLibPackage.CoreLib.Commands.Scripts.Commands;

namespace CoreLib.Commands
{
    public readonly struct CommandPair
    {
        public readonly ICommandInfo handler;
        public readonly string modName;

        public bool isServer => handler is IServerCommandHandler;
        
        public IServerCommandHandler serverHandler
        {
            get
            {
                if (handler is IServerCommandHandler serverCommandHandler)
                    return serverCommandHandler;
                return null;
            }
        }
        
        public IClientCommandHandler clientHandler
        {
            get
            {
                if (handler is IClientCommandHandler clientCommandHandler)
                    return clientCommandHandler;
                return null;
            }
        }

        public ICommandParser parser
        {
            get
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (handler is ICommandParser commandParser)
                    return commandParser;
                return null;
            }
        }
        
        public CommandOutput Execute(CommandMessage message, string[] parameters)
        {
            if (isServer)
            {
                return serverHandler.Execute(parameters, message.sender);
            }

            return clientHandler.Execute(parameters);
        }

        public CommandPair(ICommandInfo handler, string modName)
        {
            this.handler = handler;
            this.modName = modName;
        }
    }
}