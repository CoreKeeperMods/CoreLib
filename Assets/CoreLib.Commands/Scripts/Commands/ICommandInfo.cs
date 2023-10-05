namespace CoreLib.Commands
{
    /// <summary>
    /// Internal interface. Do not implement directly.
    /// </summary>
    public interface ICommandInfo
    {
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