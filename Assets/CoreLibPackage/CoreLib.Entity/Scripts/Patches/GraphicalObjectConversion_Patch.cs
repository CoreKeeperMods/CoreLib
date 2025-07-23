using System.Collections.Generic;
using System.Reflection.Emit;
using CoreLib.Submodules.ModEntity.Components;
using HarmonyLib;
using Pug.ECS.Hybrid;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Patches
{
    // ReSharper disable once InconsistentNaming
    public class GraphicalObjectConversion_Patch
    {
        
        public delegate void RefAction<in T1, T2, T3>(T1 arg1, ref T2 arg2, ref T3 arg3);
        
        [HarmonyPatch(typeof(GraphicalObjectConversion), nameof(GraphicalObjectConversion.Convert))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ChangeConversion(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Ldloc_S),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ObjectAuthoring), nameof(ObjectAuthoring.graphicalPrefab))),
                    new CodeMatch(i => i.IsStloc()));

            matcher
                .Advance(1)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloca_S, 1),
                    new CodeInstruction(OpCodes.Ldloca_S, 2)
                    )
                
                .SetInstructionAndAdvance(Transpilers.EmitDelegate<RefAction<ObjectAuthoring, Component, GameObject>>(
                    (ObjectAuthoring authoring, ref Component component, ref GameObject gameObject) =>
                {
                    var supportsPooling = authoring.GetComponent<SupportsPooling>();
                    if (supportsPooling != null)
                    {
                        component = authoring.graphicalPrefab.GetComponent<EntityMonoBehaviour>();
                    }
                    else
                    {
                        gameObject = authoring.graphicalPrefab;
                    }
                }))
                .Set(OpCodes.Nop, null);
                
            
            return matcher.InstructionEnumeration();
        }
    }
}