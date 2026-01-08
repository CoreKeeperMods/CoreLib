using System;
using QFSW.QC;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Parser
{
    /// <summary>
    /// A parser class for converting string representations of SkillID values into their corresponding enumeration values.
    /// </summary>
    /// <remarks>
    /// This class overrides the parsing functionality to handle case insensitivity and trimming of input strings.
    /// </remarks>
    [Preserve]
    public class SkillIDParser : BasicQcParser<SkillID>
    {
        /// <summary>
        /// Parses the input string into a corresponding <c>SkillID</c> enumeration value.
        /// </summary>
        /// <param name="value">The input string to parse, representing a skill identifier.</param>
        /// <returns>The parsed <c>SkillID</c> value, or the default value of <c>SkillID</c> if parsing fails.</returns>
        public override SkillID Parse(string value)
        {
            value = value.ToLower().Trim();
            Enum.TryParse(value, true, out SkillID condition);
            return condition;
        }
    }
}