using System;
using System.Linq;
using HarmonyLib;
using Unity.Entities;

namespace CoreLib.Submodules.Entity.Patches
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
                
                return type.IsValueType && typeof(IComponentData).IsAssignableFrom(type);
            });
            foreach (Type type in types)
            {
                CoreLibMod.Log.LogDebug($"Patching {nameof(EntityUtility.GetComponentData)} for type {type.FullName}");
                var originalGen = original.MakeGenericMethod(type);
                var prefixGen = prefix.MakeGenericMethod(type);
                CoreLibMod.harmony.Patch(originalGen, new HarmonyMethod(prefixGen));
            }
            CoreLibMod.harmony.PatchAll(typeof(EntityUtility_Patch));
        }

        public static void OnGetComponentData<T>(Unity.Entities.Entity entity, World world)
        {
            if (!world.EntityManager.Exists(entity))
            {
                CoreLibMod.Log.LogInfo($"Entity does not exist! Type: {typeof(T).FullName}");
                return;
            }
            
            if (!world.EntityManager.HasComponent<T>(entity))
            {
                CoreLibMod.Log.LogInfo($"Component type is: {typeof(T).FullName}");
            }
        }
    }
}