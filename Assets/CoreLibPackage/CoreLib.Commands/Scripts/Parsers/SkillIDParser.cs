using System;
using QFSW.QC;
using UnityEngine.Scripting;

namespace CoreLib.Commands.Parsers
{
    [Preserve]
    public class SkillIDParser : BasicQcParser<SkillID>
    {
        public override SkillID Parse(string value)
        {
            value = value.ToLower().Trim();
            Enum.TryParse(value, true, out SkillID condition);
            return condition;
        }
    }
}