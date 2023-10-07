using QFSW.QC;
using UnityEngine.Scripting;

namespace CoreLib.Commands.Parsers
{
    [Preserve]
    public class ObjectIDParser : BasicQcParser<ObjectID>
    {
        public override ObjectID Parse(string value)
        {
            var output = CommandUtil.ParseItemName(value, out ObjectID result);
            if (output.feedback != "")
            {
                CoreLibMod.Log.LogWarning($"Error parsing ObjectID: {output.feedback}");
            }

            return result;
        }
    }
}