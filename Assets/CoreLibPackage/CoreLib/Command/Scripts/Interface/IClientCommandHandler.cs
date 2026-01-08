using CoreLib.Submodule.Command.Data;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Interface
{
    /// <summary>
    /// Represents a handler for client-side commands.
    /// Developers can implement this interface to define custom commands executed on the client.
    /// The server may still reject the command execution based on its own logic.
    /// To ensure proper functionality, register commands using
    /// <code>CommandsModule.AddCommands(modId);</code> during the `Load` method.
    /// </summary>
    public interface IClientCommandHandler : ICommandInfo
    {
        /// <summary>
        /// Execute command and return feedback.
        /// </summary>
        /// <param name="parameters">List of arguments entered by the user.</param>
        /// <returns>Feedback as a result of the command execution.</returns>
        CommandOutput Execute(string[] parameters);
    }
}