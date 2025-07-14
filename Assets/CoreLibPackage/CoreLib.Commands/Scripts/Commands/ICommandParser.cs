namespace CoreLib.Commands.CoreLibPackage.CoreLib.Commands.Scripts.Commands
{
    public interface ICommandParser
    {
        public CommandToken[] Parse(string[] parameters);

        public bool TryAutocomplete(CommandToken token, out string newValue)
        {
            newValue = null;
            return false;
        }
    }
}