using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Interaction;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Patches
{
    /// <summary>
    /// Provides patching methods for managing and applying transformations related to interactable entities.
    /// Used in conjunction with Harmony to modify or augment the behavior of existing methods within the ModEntity submodule.
    /// </summary>
    [HarmonyPatch]
    // ReSharper disable once InconsistentNaming
    public static class InteractableConverter_Patch
    {
        /// <summary>
        /// Represents a delegate that processes a provided input and modifies the state of a second provided argument.
        /// Primarily designed for scenarios where ref-based modifications need to be performed on the second argument
        /// while maintaining read-only access to the first argument.
        /// </summary>
        /// <typeparam name="T1">The type of the first input parameter, provided as a readonly reference.</typeparam>
        /// <typeparam name="T2">The type of the second input parameter, provided as a reference for modification.</typeparam>
        /// <param name="arg1">The input parameter passed by readonly reference, used as contextual data for processing.</param>
        /// <param name="arg2">The input parameter passed by reference, allowing modifications to its state.</param>
        public delegate void RefAction<in T1, T2>(T1 arg1, ref T2 arg2);

        /// <summary>
        /// Stores the last known object information processed during conversion.
        /// This variable is used within conversion logic to temporarily hold
        /// details about the object being handled, such as its prefab tile size
        /// and corner offset.
        /// </summary>
        private static ObjectInfo lastInfo;

        /// <summary>
        /// Modifies the sequence of code instructions in the transpiler to alter the behavior of the PostConvert method in the
        /// InteractablePostConverter class. These alterations are designed to adjust object authoring and prefab handling logic.
        /// </summary>
        /// <param name="instructions">An enumerable of <see cref="CodeInstruction"/> representing the original instructions
        /// of the PostConvert method prior to being transpiled.</param>
        /// <returns>An enumerable of <see cref="CodeInstruction"/> representing the modified instruction sequence.</returns>
        [HarmonyPatch(typeof(InteractablePostConverter), "PostConvert")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ChangeConversion2(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            
            matcher.MatchForward(false,
                new CodeMatch(
                    OpCodes.Ldfld,
                    AccessTools.Field(typeof(ObjectAuthoring), nameof(ObjectAuthoring.graphicalPrefab))
                ),
                new CodeMatch(OpCodes.Ldc_I4_1));

            matcher.Advance(4)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldloca_S, 2))
                .InsertAndAdvance(Transpilers.EmitDelegate<RefAction<GameObject, List<PrefabInfo>>>(
                    (GameObject authoring, ref List<PrefabInfo> list) =>
                    {
                        lastInfo = null;
                        var objectAuthoring = authoring.GetComponent<ObjectAuthoring>();
                        if (objectAuthoring == null) return;
                        
                        MonoBehaviour ourComponent = objectAuthoring.graphicalPrefab.GetComponent<EntityMonoBehaviour>();

                        if (ourComponent == null)
                            ourComponent = objectAuthoring.graphicalPrefab.GetComponent<MonoBehaviour>();

                        if (ourComponent == null)
                        {
                            CoreLibMod.Log.LogError($"Prefab {authoring.gameObject.name} seems to have absolutely no MonoBehaviour's attached!");
                            return;
                        }
                        
                        list = new List<PrefabInfo>(1)
                        {
                            new PrefabInfo()
                            {
                                ecsPrefab = authoring.gameObject,
                                prefab = ourComponent
                            }
                        };
                        
                        var placeable = authoring.GetComponent<PlaceableObjectAuthoring>();

                        lastInfo = new ObjectInfo()
                        {
                            prefabTileSize = placeable != null ? placeable.prefabTileSize : Vector2Int.one,
                            prefabCornerOffset = placeable != null ? placeable.prefabCornerOffset : Vector2Int.zero
                        };
                    }));
            
            matcher.MatchForward(false,
                new CodeMatch(
                    OpCodes.Ldfld,
                    AccessTools.Field(typeof(PrefabInfo), nameof(PrefabInfo.prefab))
                ),
                new CodeMatch(OpCodes.Ldc_I4_1));

            matcher.Advance(3)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_1))
                .InsertAndAdvance(Transpilers.EmitDelegate<Action<GameObject>>(
                    (authoring) =>
                    {
                        var entityMono = authoring.GetComponent<EntityMonoBehaviourData>();
                        lastInfo = null;

                        if (entityMono != null)
                        {
                            lastInfo = entityMono.objectInfo;
                        }
                    }));
            
            matcher
                .MatchForward(
                    false,
                    new CodeMatch(i => i.IsLdloc()),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EntityMonoBehaviourData), nameof(EntityMonoBehaviourData.objectInfo))))
                .Repeat(matcher2 =>
                {
                    matcher2
                        .SetOpcodeAndAdvance(OpCodes.Nop)
                        .SetInstructionAndAdvance(Transpilers.EmitDelegate<Func<ObjectInfo>>(() => lastInfo));
                });
            
            
            
            return matcher.InstructionEnumeration();
        }

        //                            var placeable = authoring.GetComponent<PlaceableObjectAuthoring>();
                            //
                            // lastInfo = new ObjectInfo()
                            // {
                            //     prefabTileSize = placeable != null ? placeable.prefabTileSize : Vector2Int.one,
                            //     prefabCornerOffset = placeable != null ? placeable.prefabCornerOffset : Vector2Int.zero
                            // };
    }
}