using HarmonyLib;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;
using UnityEngine;

namespace CoreLib.Submodules.TileSet.Patches
{
	public static class TilesetTypeUtility_Patch
	{
		private static ModTileset GetTileset(int index)
		{
			Tileset tilesetId = (Tileset)index;
			if (TileSetModule.customTilesets.ContainsKey(tilesetId))
			{
				return TileSetModule.customTilesets[tilesetId];
			}
		
			if (index >= TileSetModule.modTilesetIdRangeStart)
			{
				return TileSetModule.missingTileset;
			}

			return null;
		}
	
		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetTileset))]
		[HarmonyPrefix]
		public static bool GetTileset(int index, ref PugMapTileset __result)
		{
			ModTileset tileset = GetTileset(index);
			if (tileset != null)
			{
				__result = tileset.layers;
				return false;
			}

			return true;
		}

		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetTexture))]
		[HarmonyPrefix]

		public static bool GetTexture(int tilesetIndex, LayerName layerName, TextureType textureType, ref Texture2D __result)
		{
			ModTileset tileset = GetTileset(tilesetIndex);
			if (tileset != null)
			{
				__result = null;
				if (textureType == TextureType.REGULAR)
					__result = tileset.tilesetTexture;
				else if (textureType == TextureType.EMISSIVE)
					__result = tileset.tilesetEmissiveTexture;
				return false;
			}

			return true;
		}

		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetAdaptiveTexture))]
		[HarmonyPrefix]
		public static bool GetAdaptiveTexture(int tilesetIndex, LayerName layerName, ref Texture2D __result)
		{
	    
			ModTileset tileset = GetTileset(tilesetIndex);
			if (tileset != null)
			{
				__result = null;
				if (tileset.adaptiveTilesetTextures.ContainsKey(layerName))
				{
					__result = tileset.adaptiveTilesetTextures[layerName];
				}
				return false;
			}

			return true;
		}


		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetOverrideMaterial))]
		[HarmonyPrefix]
		public static bool GetOverrideMaterial(int tilesetIndex, LayerName layerName, ref Material __result)
		{
			ModTileset tileset = GetTileset(tilesetIndex);
			if (tileset != null)
			{
				__result = null;
				if (tileset.overrideMaterials.ContainsKey(layerName))
				{
					__result = tileset.overrideMaterials[layerName];
				}
				return false;
			}

			return true;
		}
    

		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetEditorOverrideMaterial))]
		[HarmonyPrefix]
		public static bool GetEditorOverrideMaterial(int tilesetIndex, LayerName tileName, ref Material __result)
		{
			ModTileset tileset = GetTileset(tilesetIndex);
			if (tileset != null)
			{
				__result = null;
				if (tileset.overrideMaterials.ContainsKey(tileName))
				{
					__result = tileset.overrideMaterials[tileName];
				}
				return false;
			}

			return true;
		}

    
		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetOverrideParticles))]
		[HarmonyPrefix]
		public static bool GetOverrideParticles(int tilesetIndex, LayerName tileName, ref ParticleSystem __result)
		{
			ModTileset tileset = GetTileset(tilesetIndex);
			if (tileset != null)
			{
				__result = null;
				if (tileset.overrideParticles.ContainsKey(tileName))
				{
					__result = tileset.overrideParticles[tileName];
				}
				return false;
			}

			return true;
		}

    
		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetFriendlyName))]
		[HarmonyPrefix]
		public static bool GetFriendlyName(int index, ref string __result)
		{
			ModTileset tileset = GetTileset(index);
			if (tileset != null)
			{
				__result = tileset.tilesetId;
				return false;
			}

			return true;
		}
    
	}
}