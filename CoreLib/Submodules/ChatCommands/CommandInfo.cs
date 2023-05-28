using System;

namespace CoreLib.Submodules.ChatCommands
{
    public class CommandInfo
    {
        private static readonly string TAG = "COMMAND";

        public string modId;
        public string id;
        public CommandKind kind;

        public CommandInfo(string modId, string id, CommandKind kind)
        {
            this.modId = modId;
            this.id = id;
            this.kind = kind;
        }


        public override string ToString()
        {
            return $"{TAG};{modId};{id};{kind}";
        }

        public static bool TryParseInfoString(string infoStr, out CommandInfo commandInfo)
        {
            commandInfo = null;

            string[] parts = infoStr.Split(";");
            if (parts.Length != 4 ||
                !parts[0].Equals(TAG))
                return false;

            commandInfo = new CommandInfo(parts[1], parts[2], Enum.Parse<CommandKind>(parts[3]));
            return true;
        }
    }
}