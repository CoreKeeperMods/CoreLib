using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime.Attributes;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace CoreLib.Util.Jobs
{
    [Il2CppImplements(typeof(IComponentData))]
    public struct ModParameterValueProvider_IComponentData<T>
        where T : struct
    {
        ModComponentTypeHandle<T> _typeHandle;

        public void ScheduleTimeInitialize(ComponentSystemBase jobComponentSystem, bool isReadOnly)
        {
            _typeHandle = jobComponentSystem.GetModComponentTypeHandle<T>(isReadOnly);
        }

        public struct Runtime
        {
            public unsafe byte* ptr;

            public unsafe ref T For(int i)
            {
                return ref ArrayElementAsRef<T>(ptr, i);
            }
            
            public static unsafe ref TU ArrayElementAsRef<TU>(void* ptr, int index) where TU : struct
            {
                return ref Unsafe.AsRef<TU>((void*)((IntPtr)ptr + index * Unsafe.SizeOf<T>()));
            }
        }

        public unsafe Runtime PrepareToExecuteOnEntitiesIn(ref ArchetypeChunk chunk)
        {
            var componentDatas = chunk.GetNativeArray(_typeHandle);
            return new Runtime()
            {
                ptr = (byte*)componentDatas.pointer
            };
        }

        public struct StructuralChangeRuntime
        {
            public EntityManager _manager;
            public int _typeIndex;

            public unsafe T For(Entity entity, out T originalComponent)
            {
                var access = _manager.GetCheckedEntityDataAccess();
                var ecs = access->EntityComponentStore;
                originalComponent = Marshal.PtrToStructure<T>((IntPtr)ecs->GetComponentDataWithTypeRO(entity, _typeIndex));
                return originalComponent;
            }

            public unsafe void WriteBack(Entity entity, ref T lambdaComponent, ref T originalComponent)
            {
                var access = _manager.GetCheckedEntityDataAccess();
                var ecs = access->EntityComponentStore;
                // MemCmp check is necessary to ensure we only write-back the value if we changed it in the lambda (or a called function)
                if (UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref lambdaComponent), UnsafeUtility.AddressOf(ref originalComponent), Unsafe.SizeOf<T>()) != 0 &&
                    ecs->HasComponent(entity, _typeIndex))
                {
                    Marshal.StructureToPtr(lambdaComponent, (IntPtr)ecs->GetComponentDataWithTypeRW(entity, _typeIndex, ecs->GlobalSystemVersion), false);
                }
            }
        }

        public StructuralChangeRuntime PrepareToExecuteWithStructuralChanges(ComponentSystemBase componentSystem, EntityQuery query)
        {
            return new StructuralChangeRuntime() { _manager = componentSystem.EntityManager, _typeIndex = ECSExtensions.GetModTypeIndex<T>() };
        }
    }
    
    public struct ModComponentTypeHandle<T>
    {
        internal readonly int m_TypeIndex;
        internal readonly uint m_GlobalSystemVersion;
        internal readonly bool m_IsReadOnly;
        internal readonly bool m_IsZeroSized;

        public uint GlobalSystemVersion => m_GlobalSystemVersion;
        public bool IsReadOnly => m_IsReadOnly;

#pragma warning disable 0414
        private readonly int m_Length;
#pragma warning restore 0414
        
        internal unsafe ModComponentTypeHandle(bool isReadOnly, uint globalSystemVersion)
        {
            m_Length = 1;
            m_TypeIndex = ECSExtensions.GetModTypeIndex<T>();
            m_IsZeroSized = TypeManager.GetTypeInfoPointer()[m_TypeIndex & 0x00FFFFFF].IsZeroSized;
            m_GlobalSystemVersion = globalSystemVersion;
            m_IsReadOnly = isReadOnly;
        }
    }
}