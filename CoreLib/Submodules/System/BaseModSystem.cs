﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CoreLib.Submodules.ModComponent;
using Il2CppInterop.Runtime.Attributes;
using PugTilemap;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace CoreLib.Submodules.ModSystem
{
	/// <summary>
	/// Base class for modded ECS systems. Used identically to SystemBase.
	/// All methods function for any generic variants
	/// Please ensure to use methods provided in this class for safety
	/// </summary>
    public unsafe class BaseModSystem : ComponentSystemBase
    {
	    private bool tileAccessorCreated;
	    private TileAccessor tileAccessor;
	    
	    public BaseModSystem(IntPtr ptr) : base(ptr) { }

	    protected JobHandle Dependency
	    {
		    get => CheckedState()->Dependency;
		    set => CheckedState()->Dependency = value;
	    }

	    protected void CompleteDependency()
	    {
		    CheckedState()->CompleteDependency();
	    }
	    
	    public sealed override void Update()
	    {
		    SystemState* ptr = CheckedState();
		    if (Enabled && ShouldRunSystem())
		    {
			    if (!ptr->PreviouslyEnabled)
			    {
				    ptr->PreviouslyEnabled = true;
				    OnStartRunning();
			    }
			    ptr->BeforeOnUpdate();
			    WorldUnmanaged unmanaged = World.Unmanaged;
			    SystemHandleUntyped executingSystem = unmanaged.ExecutingSystem;
			    unmanaged.ExecutingSystem = ptr->m_Handle;
			    try
			    {
				    OnUpdate();
			    }
			    catch
			    {
				    ptr->AfterOnUpdate();
				    throw;
			    }
			    finally
			    {
				    unmanaged.ExecutingSystem = executingSystem;
			    }
			    ptr->AfterOnUpdate();
			    return;
		    }
		    if (ptr->PreviouslyEnabled)
		    {
			    ptr->PreviouslyEnabled = false;
			    OnStopRunning();
		    }
	    }

	    protected virtual void OnUpdate()
	    {
		    tileAccessorCreated = false;
	    }
	    
	    [HideFromIl2Cpp]
	    public ref RewindableAllocator WorldUpdateAllocator
	    {
		    get
		    {
			    ref var impl = ref Unsafe.AsRef<WorldUnmanagedImpl>(World.Unmanaged.m_Impl);
			    return ref Unsafe.AsRef<RewindableAllocator>(impl.UpdateAllocator);
		    }
	    }
	    
	    [HideFromIl2Cpp]
	    public new ref Unity.Core.TimeData Time
	    {
		    get
		    {
			    ref var impl = ref Unsafe.AsRef<WorldUnmanagedImpl>(World.Unmanaged.m_Impl);
			    return ref impl.CurrentTime;
		    }
	    }
	    
	    protected TileAccessor GetTileAccessor(bool readOnly = true)
	    {
		    if (!tileAccessorCreated)
		    {
			    tileAccessorCreated = true;
			    tileAccessor = new TileAccessor()
			    {
				    subMapMap = World.GetExistingSystem<UpdateSubMapSystem>().SubMapMap,
				    tilePriorityLookup = TileTypeUtility.GetSurfacePriorityLookup(WorldUpdateAllocator.ToAllocator),
				    bufferFromEntity = base.GetBufferFromEntity<SubMapLayerBuffer>(readOnly),
				    lastSubMapEntity = Entity.Null,
				    lastSubMapPos = int.MinValue,
			    };
		    }
		    return tileAccessor;
	    }
	    
	    [HideFromIl2Cpp]
	    protected internal T GetComponent<T>(Entity entity) where T : unmanaged
	    {
		    return EntityManager.GetModComponentData<T>(entity);
	    }

	    [HideFromIl2Cpp]
	    protected internal void SetComponent<T>(Entity entity, T component) where T : unmanaged
	    {
		    EntityManager.SetModComponentData(entity, component);
	    }

	    [HideFromIl2Cpp]
	    protected internal bool HasComponent<T>(Entity entity) where T : unmanaged
	    {
		    return EntityManager.HasModComponent<T>(entity);
	    }

	    [HideFromIl2Cpp]
	    public ModDynamicBuffer<T> GetBuffer<T>(Entity entity, bool isReadOnly = false) where T : unmanaged
	    {
		    SystemState* state = CheckedState();
		    
		    state->AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
		    return EntityManager.GetModBuffer<T>(entity, isReadOnly);
	    }

	    public new StorageInfoFromEntity GetStorageInfoFromEntity()
	    {
		    return base.GetStorageInfoFromEntity();
	    }

	    [HideFromIl2Cpp]
	    public new ModComponentTypeHandle<T> GetComponentTypeHandle<T>(bool isReadOnly = false) where T : unmanaged
		{
			SystemState* state = CheckedState();
			
			state->AddReaderWriter(isReadOnly ? ComponentModule.ReadOnly<T>() : ComponentModule.ReadWrite<T>());
			return EntityManager.GetModComponentTypeHandle<T>(isReadOnly);
		}

	    [HideFromIl2Cpp]
        public new ModBufferTypeHandle<T> GetBufferTypeHandle<T>(bool isReadOnly = false) where T : unmanaged
		{
			SystemState* state = CheckedState();
			
			state->AddReaderWriter(isReadOnly ? ComponentModule.ReadOnly<T>() : ComponentModule.ReadWrite<T>());
			return EntityManager.GetModBufferTypeHandle<T>(isReadOnly);
		}
        
        [HideFromIl2Cpp]
        public new ModComponentDataFromEntity<T> GetComponentDataFromEntity<T>(bool isReadOnly = false) where T : unmanaged
		{
			SystemState* state = CheckedState();
			
			state->AddReaderWriter(isReadOnly ? ComponentModule.ReadOnly<T>() : ComponentModule.ReadWrite<T>());
			return new ModComponentDataFromEntity<T>(EntityManager);
		}

        [HideFromIl2Cpp]
		public new ModBufferFromEntity<T> GetBufferFromEntity<T>(bool isReadOnly = false) where T : unmanaged
		{
			SystemState* state = CheckedState();
			
			state->AddReaderWriter(isReadOnly ? ComponentModule.ReadOnly<T>() : ComponentModule.ReadWrite<T>());
			return new ModBufferFromEntity<T>(EntityManager, isReadOnly);
		}
		
		[HideFromIl2Cpp]
		public new void RequireSingletonForUpdate<T>()
		{
			SystemState* state = CheckedState();
			
			ComponentType type = ComponentModule.ReadOnly<T>();
			EntityQuery singletonEntityQueryInternal = state->GetSingletonEntityQueryInternal(type);
			state->RequireForUpdate(singletonEntityQueryInternal);
		}
		
		[HideFromIl2Cpp]
		public new bool HasSingleton<T>()
		{
			SystemState* state = CheckedState();
			
			ComponentType type = ComponentModule.ReadOnly<T>();
			return state->GetSingletonEntityQueryInternal(type).CalculateEntityCount() == 1;
		}

		[HideFromIl2Cpp]
		public new T GetSingleton<T>() where T : unmanaged
		{
			SystemState* state = CheckedState();
			
			ComponentType type = ComponentModule.ReadOnly<T>();
			return state->GetSingletonEntityQueryInternal(type).GetModSingleton<T>();
		}

		[HideFromIl2Cpp]
		public new bool TryGetSingleton<T>(out T value) where T : unmanaged
		{
			SystemState* state = CheckedState();
			
			ComponentType type = ComponentModule.ReadOnly<T>();
			EntityQuery singletonEntityQueryInternal = state->GetSingletonEntityQueryInternal(type);
			bool flag = singletonEntityQueryInternal.CalculateEntityCount() == 1;
			value = (flag ? singletonEntityQueryInternal.GetModSingleton<T>() : default);
			return flag;
		}

		[HideFromIl2Cpp]
		public new void SetSingleton<T>(T value) where T : unmanaged
		{
			SystemState* state = CheckedState();
			
			ComponentType type = ComponentModule.ReadWrite<T>();
			state->GetSingletonEntityQueryInternal(type).SetModSingleton(value);
		}

		[HideFromIl2Cpp]
		public new Entity GetSingletonEntity<T>()
		{
			SystemState* state = CheckedState();
			
			ComponentType type = ComponentModule.ReadWrite<T>();
			return state->GetSingletonEntityQueryInternal(type).GetSingletonEntity();
		}

		[HideFromIl2Cpp]
		public new bool TryGetSingletonEntity<T>(out Entity value)
		{
			SystemState* state = CheckedState();
			
			ComponentType type = ComponentModule.ReadWrite<T>();
			EntityQuery singletonEntityQueryInternal = state->GetSingletonEntityQueryInternal(type);
			bool flag = singletonEntityQueryInternal.CalculateEntityCount() == 1;
			value = (flag ? singletonEntityQueryInternal.GetSingletonEntity() : Entity.Null);
			return flag;
		}
    }
}