using CoreLib.Submodule.Command.Interface;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Data
{
    /// Represents a pair consisting of a command handler and its associated module name.
    public struct CommandPair
    {
        /// Represents the command handler associated with a command pair.
        /// <remarks>
        /// This field holds an instance of a class or struct that implements the <see cref="ICommandInfo"/> interface.
        /// The handler is responsible for providing details about the command, such as its description and trigger names,
        /// and determines whether the command is executed on the server or client side.
        /// Depending on the implementation, this handler may optionally implement either <see cref="IServerCommandHandler"/>
        /// or <see cref="IClientCommandHandler"/> to represent server or client-specific command functionality.
        /// </remarks>
        /// <value>
        /// An object implementing the <see cref="ICommandInfo"/> interface, representing the command's handler.
        /// </value>
        public ICommandInfo handler;

        /// Specifies the name of the modification (mod) that the command handler is associated with.
        /// <remarks>
        /// This field identifies which mod the command handler belongs to, allowing the separation
        /// and organization of commands based on their associated modifications.
        /// </remarks>
        /// <value>
        /// A string representing the name of the associated modification.
        /// </value>
        public string modName;

        /// Indicates whether the command associated with the current handler is a server-side command.
        /// <remarks>
        /// This property evaluates the type of the <see cref="ICommandInfo"/> handler to determine if it
        /// implements the <see cref="IServerCommandHandler"/> interface. If the handler corresponds to a
        /// server-side command, this property returns true; otherwise, it returns false.
        /// </remarks>
        /// <value>
        /// A boolean value indicating whether the command is executed on the server (true) or not (false).
        /// </value>
        public bool IsServer => handler is IServerCommandHandler;

        /// Gets the associated <see cref="IServerCommandHandler"/> of the current command, if applicable.
        /// <remarks>
        /// This property retrieves the <see cref="IServerCommandHandler"/> implementation if the
        /// underlying <see cref="ICommandInfo"/> handler is of type <see cref="IServerCommandHandler"/>.
        /// If the handler does not implement <see cref="IServerCommandHandler"/>, the property will return null.
        /// </remarks>
        /// <value>
        /// An instance of <see cref="IServerCommandHandler"/> if the handler is a server command handler; otherwise, null.
        /// </value>
        public IServerCommandHandler ServerHandler
        {
            get
            {
                if (handler is IServerCommandHandler serverCommandHandler)
                    return serverCommandHandler;
                return null;
            }
        }

        /// Gets the associated <see cref="IClientCommandHandler"/> of the current command.
        /// <remarks>
        /// This property retrieves the <see cref="IClientCommandHandler"/> implementation if the
        /// underlying <see cref="ICommandInfo"/> handler is of type <see cref="IClientCommandHandler"/>.
        /// If the handler does not implement <see cref="IClientCommandHandler"/>, the property will return null.
        /// </remarks>
        /// <value>
        /// An instance of <see cref="IClientCommandHandler"/> if the handler is a client command handler; otherwise, null.
        /// </value>
        public IClientCommandHandler ClientHandler
        {
            get
            {
                if (handler is IClientCommandHandler clientCommandHandler)
                    return clientCommandHandler;
                return null;
            }
        }

        /// Executes the associated command using the provided message and parameters.
        /// <param name="message">The message containing sender information and other relevant data.</param>
        /// <param name="parameters">The parameters for the command execution.</param>
        /// <returns>The output of the command execution, including feedback and execution status.</returns>
        public CommandOutput Execute(CommandMessage message, string[] parameters)
        {
            return IsServer ? ServerHandler.Execute(parameters, message.sender) : ClientHandler.Execute(parameters);
        }

        /// Represents a pair of command data, including its handler and the mod it belongs to.
        public CommandPair(ICommandInfo handler, string modName)
        {
            this.handler = handler;
            this.modName = modName;
        }
    }
}