// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command
{
    /// <summary>
    /// Defines the contract for command information, including descriptions and trigger names.
    /// </summary>
    public interface ICommandInfo
    {
        /// <summary>
        /// Returns a detailed description of commands actions and usage.
        /// </summary>
        /// <returns>
        /// A string containing the detailed description of the command.
        /// </returns>
        string GetDescription();

        /// <summary>
        /// Returns all command trigger names associated with the handler.
        /// These trigger names are used to identify and execute the corresponding command action.
        /// </summary>
        /// <returns>
        /// An array of strings representing the trigger names for this command handler.
        /// </returns>
        string[] GetTriggerNames();
    }
}