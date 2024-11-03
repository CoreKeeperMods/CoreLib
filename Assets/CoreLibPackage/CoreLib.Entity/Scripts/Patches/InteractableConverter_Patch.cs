using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Interaction;
using Pug.ECS.Hybrid;
using UnityEngine;

namespace CoreLib.Submodules.ModEntity.Patches
{
    [HarmonyPatch]
    public static class InteractableConverter_Patch
    {
        public delegate void RefAction<in T1, T2>(T1 arg1, ref T2 arg2);
        private static ObjectInfo lastInfo;

        [HarmonyPatch(typeof(LocalInteractableConverter), "Convert")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ChangeConversion(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions)
                .MatchForward(false,
                    new CodeMatch(
                        OpCodes.Ldfld,
                        AccessTools.Field(typeof(ObjectInfo), nameof(ObjectInfo.prefabInfos))
                    ));

            matcher
                .Advance(2)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Ldarg_1)
                )
                .InsertAndAdvance(Transpilers.EmitDelegate<Func<List<PrefabInfo>, LocalInteractableAuthoring, List<PrefabInfo>>>(
                    (list, authoring) =>
                    {
                        var objectAuthoring = authoring.GetComponent<ObjectAuthoring>();
                        if (objectAuthoring == null) return list;
                        
                        MonoBehaviour ourComponent = objectAuthoring.graphicalPrefab.GetComponent<EntityMonoBehaviour>();

                        if (ourComponent == null)
                            ourComponent = objectAuthoring.graphicalPrefab.GetComponent<MonoBehaviour>();

                        if (ourComponent == null)
                        {
                            CoreLibMod.Log.LogError($"Prefab {authoring.gameObject.name} seems to have absolutely no MonoBehaviour's attached!");
                            return list;
                        }

                        list = new List<PrefabInfo>(1)
                        {
                            new PrefabInfo()
                            {
                                ecsPrefab = authoring.gameObject,
                                prefab = ourComponent
                            }
                        };

                        return list;
                    }))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_0));

           return matcher.InstructionEnumeration();
        }

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