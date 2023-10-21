using System;
using QFSW.QC;
using UnityEngine.Scripting;

namespace CoreLib.Commands.Parsers
{
    [Preserve]
    public class ConditionIDParser : BasicQcParser<ConditionID>
    {
        public override ConditionID Parse(string value)
        {
            value = value.ToLower().Trim();
            Enum.TryParse(value, true, out ConditionID condition);
            return condition;
        }
    }
}