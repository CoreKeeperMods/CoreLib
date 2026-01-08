// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: PooledGraphicalObjectConverter.cs
// Author: Minepatcher, Limoka, Moorowl
// Created: 2025-11-19
// Description: Converts a pooled graphical object into an ECS entity.
// ========================================================

/* Edited from Moorowl's Paintable Double Chest https://mod.io/g/corekeeper/m/doublechest#description */
using CoreLib.Submodule.Entity.Component;
using Pug.ECS.Hybrid;
using PugConversion;
using Unity.Mathematics;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Converter {
	public class PooledGraphicalObjectConverter : PugConverter {
		
		public override void Convert(GameObject authoring) {
			if (IsServer || !authoring.TryGetComponent(out ObjectAuthoring objectAuthoring)) return;
            
			var objectInfo = objectAuthoring.ObjectInfo;
            var prefab = objectAuthoring.graphicalPrefab;
            if(prefab == null || !prefab.TryGetComponent(out MonoBehaviour prefabComponent) ||
               !prefab.TryGetComponent(out SupportsPooling _)) return;
			
			var entity = CreateAdditionalEntity();
			float2 prefabSize = (Vector2) objectInfo.prefabTileSize;
			var prefabOffset = (float2) (Vector2) objectInfo.prefabCornerOffset - 0.5f;
			float4 renderBounds = new(prefabOffset, prefabOffset + prefabSize);
            if (authoring.TryGetComponent(out OverrideNetworkSyncDistanceAuthoring overrideNetworkSyncDistanceAuthoring))
            {
                float y = overrideNetworkSyncDistanceAuthoring.distance - 17.210213f;
                renderBounds.xy = math.min(renderBounds.xy,  -y);
                renderBounds.zw = math.max(renderBounds.zw, y);
            }
            else if (authoring.TryGetComponent(out PlayerAuthoring _))
                renderBounds = new float4(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);
			AddComponentData(entity, new GraphicalObjectPrefabCD {
				RenderBounds = renderBounds,
				PrefabComponent = prefabComponent,
				Prefab = prefab
			});
			AddComponentData(entity, new GraphicalObjectPrefabEntityCD {
				Value = PrimaryEntity
			});
			EnsureHasComponent<EntityMonoBehaviourCD>(PrimaryEntity);
		}
	}
}