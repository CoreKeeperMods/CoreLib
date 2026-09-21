// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Interface
{
    /// Defines the contract for command information, including descriptions and trigger names.
    public interface ICommandInfo
    {
        /// Returns a detailed description of commands actions and usage.
        /// <returns>
        /// A string containing the detailed description of the command.
        /// </returns>
        string GetDescription();

        /// Returns all command trigger names associated with the handler.
        /// These trigger names are used to identify and execute the corresponding command action.
        /// <returns>
        /// An array of strings representing the trigger names for this command handler.
        /// </returns>
        string[] GetTriggerNames();
    }
}