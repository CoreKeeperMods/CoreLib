namespace CoreLib.Submodules.ChatCommands
{
    /// <summary>
    /// Mark your chat command with cheat level. This information will be used to prevent players from cheating on public servers
    /// </summary>
    public enum CommandKind
    {
        /// <summary>
        /// This command only provides information and otherwise does not give player any advantage
        /// </summary>
        Info,
        /// <summary>
        /// This command is a cheat that ONLY applies to the player itself. For example: give skills
        /// </summary>
        SelfCheat,
        /// <summary>
        /// This command is a cheat that can do anything
        /// </summary>
        Cheat
    }
}