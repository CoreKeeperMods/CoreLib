using CoreLib.Submodule.Command.Util;
using QFSW.QC;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Parser
{
    /// Parses an ObjectID from a string input based on defined parsing rules and utility methods.
    /// <remarks>
    /// Utilizes the CommandUtil class for detailed parsing of item names while providing feedback if parsing errors occur.
    /// Inherits from BasicQcParser for integration into the parsing framework.
    /// </remarks>
    [Preserve]
    public class ObjectIDParser : BasicQcParser<ObjectID>
    {
        /// Parses the provided string input to an ObjectID based on defined parsing rules and utility methods.
        /// <param name="value">The string input to be parsed into an ObjectID.</param>
        /// <returns>The parsed ObjectID if successful, or a default ObjectID value if parsing fails.</returns>
        public override ObjectID Parse(string value)
        {
            var output = CommandUtil.ParseItemName(value, out ObjectID result);
            if (output.feedback != "")
            {
                CommandModule.log.LogWarning($"Error parsing ObjectID: {output.feedback}");
            }

            return result;
        }
    }
}