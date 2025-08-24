using System;
using QFSW.QC;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Parsers
{
    /// <summary>
    /// A specialized parser designed to convert string values into `ConditionID` enumerations.
    /// </summary>
    /// <remarks>
    /// The `ConditionIDParser` processes input strings by normalizing them (trimming whitespace and converting to lowercase)
    /// and attempts to map the processed strings to `ConditionID` enumeration values.
    /// This is particularly useful in scenarios where a flexible and human-readable input format is required.
    /// </remarks>
    [Preserve]
    public class ConditionIDParser : BasicQcParser<ConditionID>
    {
        /// Parses a string value and attempts to convert it to a `ConditionID` enum.
        /// The input string is processed by trimming whitespace and converting to lowercase before parsing.
        /// If the string cannot be successfully parsed, the returned `ConditionID` will have the default enum value.
        /// <param name="value">The input string to be parsed into a `ConditionID` enum value.</param>
        /// <return>Returns the parsed `ConditionID` enum value. If parsing fails, the default enum value for `ConditionID` is returned.</return>
        public override ConditionID Parse(string value)
        {
            value = value.ToLower().Trim();
            Enum.TryParse(value, true, out ConditionID condition);
            return condition;
        }
    }
}