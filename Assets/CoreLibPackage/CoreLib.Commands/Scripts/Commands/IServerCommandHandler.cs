using Unity.Entities;

namespace CoreLib.Commands
{
    /// <summary>
    /// Define a server sided command by implementing this interface.
    /// This command is always executed on the server and is subject to permission system
    /// Don't forget to register your commands by calling <code>CommandsModule.AddCommands(modId);</code> in your `Load` method.
    /// </summary>
    public interface IServerCommandHandler : ICommandInfo
    {
        /// <summary>
        /// Execute command & return feedback
        /// </summary>
        /// <param name="parameters">List of arguments entered by user</param>
        /// <param name="sender">Connection entity of the user who issued the command</param>
        /// <returns>Command message feedback</returns>
        CommandOutput Execute(string[] parameters, Entity sender);
    }
}