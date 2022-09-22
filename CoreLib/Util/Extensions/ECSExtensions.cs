using System;
using System.Runtime.InteropServices;
using Unity.Entities;

namespace CoreLib.Util.Extensions
{
    public static class ECSExtensions
    {
        /// <summary>
        /// Get Component Data of type.
        /// Experimental method to bypass lack of AOT compiled method.
        /// </summary>
        /// <typeparam name="T">Component Type</typeparam>
        public static unsafe T GetComponentDataRaw<T>(this EntityManager entityManager, Entity entity)
        {
            ComponentType ct = ComponentType.ReadOnly<T>();
            var dataAccess = entityManager.GetCheckedEntityDataAccess();
            if (!dataAccess->IsInExclusiveTransaction)
            {
                dataAccess->DependencyManager->CompleteWriteDependency(ct.TypeIndex);
            }

            byte* ret = dataAccess->EntityComponentStore->GetComponentDataWithTypeRO(entity, ct.TypeIndex);

            return Marshal.PtrToStructure<T>((IntPtr)ret);
        }

        /// <summary>
        /// Set Component Data of type.
        /// Experimental method to bypass lack of AOT compiled method.
        /// </summary>
        /// <param name="component">data to write</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static unsafe void SetComponentDataRaw<T>(this EntityManager entityManager, Entity entity, T component)
        {
            int typeIndex = ComponentType.ReadWrite<T>().TypeIndex;
            var dataAccess = entityManager.GetCheckedEntityDataAccess();
            var componentStore = dataAccess->EntityComponentStore;
            
            if (!dataAccess->IsInExclusiveTransaction)
            {
                dataAccess->DependencyManager->CompleteReadAndWriteDependency(typeIndex);
            }

            byte* writePtr = componentStore->GetComponentDataWithTypeRW(entity, typeIndex, componentStore->m_GlobalSystemVersion);
            Marshal.StructureToPtr(component, (IntPtr)writePtr, false);
        }
    }
}