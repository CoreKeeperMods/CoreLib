namespace CoreLib.Commands
{
    /// <summary>
    /// Define a client sided command by implementing this interface.
    /// This command is always executed on the client, but server might still refuse to execute it.
    /// Don't forget to register your commands by calling <code>CommandsModule.AddCommands(modId);</code> in your `Load` method.
    /// </summary>
    public interface IClientCommandHandler : ICommandInfo
    {
        /// <summary>
        /// Execute command & return feedback
        /// </summary>
        /// <param name="parameters">List of arguments entered by user</param>
        /// <returns>Command message feedback</returns>
        CommandOutput Execute(string[] parameters);
    }
}