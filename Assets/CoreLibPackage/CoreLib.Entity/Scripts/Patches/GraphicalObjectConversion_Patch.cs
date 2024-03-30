using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using CoreLib.Submodules.ModEntity.Components;
using HarmonyLib;
using Pug.ECS.Hybrid;

namespace CoreLib.Submodules.ModEntity.Patches
{
    public class GraphicalObjectConversion_Patch
    {
        
        [HarmonyPatch(typeof(GraphicalObjectConversion), nameof(GraphicalObjectConversion.Convert))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ChangeConversion(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Ldloc_0),
                    new CodeMatch(OpCodes.Ldloc_3),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ObjectAuthoring), nameof(ObjectAuthoring.graphicalPrefab))),
                    new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(GraphicalObjectPrefabCD), nameof(GraphicalObjectPrefabCD.Prefab))));

            matcher
                .Advance(2)
                .SetInstructionAndAdvance(Transpilers.EmitDelegate<Action<GraphicalObjectPrefabCD, ObjectAuthoring>>((graphicalObject, authoring) =>
                {
                    var supportsPooling = authoring.GetComponent<SupportsPooling>();
                    if (supportsPooling != null)
                    {
                        graphicalObject.PrefabComponent = authoring.graphicalPrefab.GetComponent<EntityMonoBehaviour>();
                    }
                    else
                    {
                        graphicalObject.Prefab = authoring.graphicalPrefab;
                    }
                }))
                .Set(OpCodes.Nop, null);
                
            
            return matcher.InstructionEnumeration();
        }
    }
}