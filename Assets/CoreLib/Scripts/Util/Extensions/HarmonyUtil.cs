using System;
using System.Reflection;
using HarmonyLib;
using MemberInfo = PugMod.MemberInfo;

namespace CoreLib.Util.Extensions
{
    public static class HarmonyUtil
    {
        private static Harmony harmony = new Harmony("mods");
        
        public static void PatchAll(Type type)
        {
            harmony.PatchAll(type);
        }

        public static void Patch(MemberInfo original,
            MemberInfo prefix = null,
            MemberInfo postfix = null,
            MemberInfo transpiler = null,
            MemberInfo finalizer = null,
            MemberInfo ilmanipulator = null)
        {
            harmony.Patch((MethodBase)original, 
                prefix != null ? new HarmonyMethod((MethodInfo)prefix) : null, 
                postfix != null ? new HarmonyMethod((MethodInfo)postfix) : null, 
                transpiler != null ? new HarmonyMethod((MethodInfo)transpiler) : null, 
                finalizer != null ? new HarmonyMethod((MethodInfo)finalizer) : null, 
                ilmanipulator != null ? new HarmonyMethod((MethodInfo)ilmanipulator) : null);
        }
    }
}