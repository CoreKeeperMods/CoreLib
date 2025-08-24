using System.Collections.Generic;
using System.Reflection.Emit;
using CoreLib.Submodule.Entity.Components;
using HarmonyLib;
using Pug.ECS.Hybrid;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Patches
{
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// This class serves as a patch for modifying or extending the behavior of graphical object conversion within the associated module.
    /// It is registered through the patching mechanism defined in the core library.
    /// </summary>
    public class GraphicalObjectConversion_Patch
    {
        /// <summary>
        /// Represents a delegate type that takes an input parameter of type <typeparamref name="T1"/> as a "read-only reference",
        /// and two parameters of types <typeparamref name="T2"/> and <typeparamref name="T3"/> as "modifiable references".
        /// The delegate is used to execute operations involving these parameters.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed as a read-only reference to the delegate.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed as a modifiable reference to the delegate.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed as a modifiable reference to the delegate.</typeparam>
        public delegate void RefAction<in T1, T2, T3>(T1 arg1, ref T2 arg2, ref T3 arg3);

        /// <summary>
        /// Modifies the IL code of the <c>GraphicalObjectConversion.Convert</c> method
        /// by injecting custom logic into the method's execution using the Harmony transpiler.
        /// </summary>
        /// <param name="instructions">The original IL code instructions of the <c>Convert</c> method.</param>
        /// <returns>An enumerable collection of modified IL code instructions, with custom logic injected.</returns>
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