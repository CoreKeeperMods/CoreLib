
namespace CoreLib.Submodules.ChatCommands
{
    /// <summary>
    /// Define a chat command by implementing this interface.
    /// Don't forget to register your commands by calling <code>CommandsModule.AddCommands(Assembly.GetExecutingAssembly());</code> in your `Load` method.
    /// </summary>
    public interface IChatCommandHandler
    {

        /// <summary>
        /// Execute command & return feedback
        /// </summary>
        /// <param name="parameters">List of arguments entered by user</param>
        /// <returns>Command message feedback</returns>
        CommandOutput Execute(string[] parameters);
    
        /// <summary>
        /// Returns detailed description of commands actions and usage
        /// </summary>
        string GetDescription();
        /// <summary>
        /// Returns all command names that trigger this handler
        /// </summary>
        string[] GetTriggerNames();
    }
}