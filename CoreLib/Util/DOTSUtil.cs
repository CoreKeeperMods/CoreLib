using System;
using CoreLib.Util.Extensions;
using Il2CppInterop.Runtime;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace CoreLib
{
    public static class DOTSUtil
    {
        /// <summary>
        /// This function allows for unregistered component types to be added to the TypeManager allowing for their use
        /// across the ECS apis _after_ TypeManager.Initialize() may have been called. Importantly, this function must
        /// be called from the main thread and will create a synchronization point across all worlds. If a type which
        /// is already registered with the TypeManager is passed in, this function will throw.
        /// </summary>
        /// <remarks>Types with [WriteGroup] attributes will be accepted for registration however their
        /// write group information will be ignored.</remarks>
        /// <param name="types"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static unsafe void AddNewComponentTypes(params Il2CppSystem.Type[] types)
        {
            // We might invalidate the SharedStatics ptr so we must synchronize all jobs that might be using those ptrs
            foreach (var world in World.All)
                world.EntityManager.BeforeStructuralChange();

            // Is this a new type, or are we replacing an existing one?
            foreach (var type in types)
            {
                if (TypeManager.s_ManagedTypeToIndex.ContainsKey(type))
                    continue;

                var typeInfo = TypeManager.BuildComponentType(type);
                TypeManager.AddTypeInfoToTables(type, typeInfo, type.FullName);
            }

            // We may have added enough types to cause the underlying containers to resize so re-fetch their ptrs
            TypeManager.SharedEntityOffsetInfo.Ref.GetData() = TypeManager.s_EntityOffsetList.GetMListData()->Ptr;
            TypeManager.SharedBlobAssetRefOffset.Ref.GetData() = TypeManager.s_BlobAssetRefOffsetList.GetMListData()->Ptr;
            TypeManager.SharedWriteGroup.Ref.GetData() = TypeManager.s_WriteGroupList.GetMListData()->Ptr;

            // Since the ptrs may have changed we need to ensure all entity component stores are using the correct ones
            foreach (var w in World.All)
            {
                var access = w.EntityManager.GetCheckedEntityDataAccess();
                var ecs = access->EntityComponentStore;
                ecs->InitializeTypeManagerPointers();
            }
        }
        
        public static unsafe ref T GetData<T>(this SharedStatic<T> shared) where T : new() 
        {
            IntPtr field = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<SharedStatic<T>>.NativeClassPtr, "_buffer");
            
            IntPtr intPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(shared) + (int)IL2CPP.il2cpp_field_get_offset(field);
            IntPtr intPtr2 = *(IntPtr*)intPtr;
            
            return ref System.Runtime.CompilerServices.Unsafe.AsRef<T>((void*)intPtr2);
        }

        public static unsafe UnsafeList* GetMListData<T>(this NativeList<T> list) where T : new()
        {
            IntPtr field = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<NativeList<T>>.NativeClassPtr, "m_ListData");
            
            IntPtr intPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(list) + (int)IL2CPP.il2cpp_field_get_offset(field);
            IntPtr intPtr2 = *(IntPtr*)intPtr;
            return (UnsafeList*)intPtr2;
        }
    }
}