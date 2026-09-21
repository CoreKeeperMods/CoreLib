using HarmonyLib;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.TileSet.Patch
{/* TODO commented out, needs rework
	/// Provides utility methods to override specific functionalities in the TilesetTypeUtility class through Harmony patches.
	public static class TilesetTypeUtilityPatch
	{
		/// Retrieves a ModTileset object corresponding to the given index.
		private static ModTileset GetTileset(int index)
		{
			var tilesetId = (Tileset)index;
			if (TileSetModule.customTilesets.TryGetValue(tilesetId, out var tileset))
                return tileset;
		
            return index >= TileSetModule.MOD_TILESET_ID_RANGE_START ? TileSetModule.missingTileset : null;
        }

		/// Retrieves a tileset by its index and attempts to populate the result with the corresponding tileset layers if available.
		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(TilesetTypeUtility.GetTileset))]
		[HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        public static bool GetTileset(int index, ref PugMapTileset __result)
		{
			var tileset = GetTileset(index);
            if (tileset == null) return true;
            __result = tileset.layers;
            return false;

        }

		/// Retrieves the texture for a specific layer and texture type within a tileset if it exists.
		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(TilesetTypeUtility.GetTexture))]
		[HarmonyPrefix]

        // ReSharper disable once InconsistentNaming
        public static bool GetTexture(int tilesetIndex, LayerName layerName, TextureType textureType, ref Texture2D __result)
		{
			var tileset = GetTileset(tilesetIndex);
            if (tileset == null) return true;
            __result = tileset.tilesetTextures.GetTexture(textureType);
            return false;

        }

		/// Retrieves the adaptive texture for the specified layer and texture type within a tileset if available.
		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(TilesetTypeUtility.GetAdaptiveTexture))]
		[HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        public static bool GetAdaptiveTexture(int tilesetIndex, LayerName layerName, TextureType textureType, ref Texture2D __result)
		{
			var tileset = GetTileset(tilesetIndex);
            if (tileset == null) return true;
            __result = null;

            if (tileset.adaptiveTilesetTextures.TryGetValue(layerName, out var texture))
            {
	            __result = texture.texture;
            }
            
            return false;

        }


		/// Retrieves the override material for a specified layer within a tileset if it is defined.
		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(TilesetTypeUtility.GetOverrideMaterial))]
		[HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        public static bool GetOverrideMaterial(int tilesetIndex, LayerName layerName, ref Material __result)
		{
			var tileset = GetTileset(tilesetIndex);
            if (tileset == null) return true;
            __result = null;
            var overrideMaterial = tileset.overrideMaterials.Find(x => x.layerName == layerName);
            if (overrideMaterial != null) __result = overrideMaterial.overrideMaterial;
            return false;
        }


		/// Retrieves the override material for a specific layer name within a tileset in the editor, if it is defined.
		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(TilesetTypeUtility.GetEditorOverrideMaterial))]
		[HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        public static bool GetEditorOverrideMaterial(int tilesetIndex, LayerName tileName, ref Material __result)
		{
			var tileset = GetTileset(tilesetIndex);
            if (tileset == null) return true;
            __result = null;
            var overrideMaterial = tileset.overrideMaterials.Find(x => x.layerName == tileName);
            if (overrideMaterial != null) __result = overrideMaterial.editorOverrideMaterial;
            return false;
        }


		/// Retrieves an override particle system for a specific layer name within a tileset, if it is defined.
		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(TilesetTypeUtility.GetOverrideParticles))]
		[HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        public static bool GetOverrideParticles(int tilesetIndex, LayerName tileName, ref ParticleSystem __result)
		{
			var tileset = GetTileset(tilesetIndex);
            if (tileset == null) return true;
            __result = null;
            var overrideParticle = tileset.overrideParticles.Find(x => x.layerName == tileName);
            if (overrideParticle != null) __result = overrideParticle.overrideParticlePrefab;
            return false; 
        }


		/// Gets the friendly name of a tileset based on its index.
		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(TilesetTypeUtility.GetFriendlyName))]
		[HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        public static bool GetFriendlyName(int index, ref string __result)
		{
			var tileset = GetTileset(index);
            if (tileset == null) return true;
            __result = tileset.tilesetId;
            return false;
        }
	}*/
}