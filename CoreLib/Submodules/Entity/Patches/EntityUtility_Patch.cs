using System;
using System.Linq;
using CoreLib.Submodules.ModComponent;
using HarmonyLib;
using Il2CppInterop.Runtime;
using PugTilemap;
using Unity.Entities;
using UnityEngine;

namespace CoreLib.Submodules.ModEntity.Patches
{
    public static class EntityUtility_Patch
    {
        public static void ApplyPatch()
        {
            var original = typeof(EntityUtility).GetMethod(nameof(EntityUtility.GetComponentData));
            var prefix = typeof(EntityUtility_Patch).GetMethod(nameof(OnGetComponentData));
            
            var types = AccessTools.AllTypes().Where(type =>
            {
                if (!type.Assembly.FullName.Contains("Pug")) return false;
                
                Il2CppSystem.Type il2CppType = GetIl2CppTypeSafe(type);
                return type.IsValueType && il2CppType != null && il2CppType.ImplementInterface(Il2CppType.Of<IComponentData>());
            });
            foreach (Type type in types)
            {
                CoreLibPlugin.Logger.LogDebug($"Patching {nameof(EntityUtility.GetComponentData)} for type {type.FullName}");
                var originalGen = original.MakeGenericMethod(type);
                var prefixGen = prefix.MakeGenericMethod(type);
                CoreLibPlugin.harmony.Patch(originalGen, new HarmonyMethod(prefixGen));
            }
            CoreLibPlugin.harmony.PatchAll(typeof(EntityUtility_Patch));
        }

        private static Il2CppSystem.Type GetIl2CppTypeSafe(Type type)
        {
            try
            {
                return Il2CppType.From(type, false);
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        public static void OnGetComponentData<T>(Entity entity, World world)
        {
            if (!world.EntityManager.Exists(entity))
            {
                CoreLibPlugin.Logger.LogInfo($"Entity does not exist! Type: {typeof(T).FullName}");
                return;
            }
            
            if (!world.EntityManager.HasModComponent<T>(entity))
            {
                CoreLibPlugin.Logger.LogInfo($"Component type is: {typeof(T).FullName}");
            }
        }
    }
}