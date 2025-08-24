using HarmonyLib;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.TileSets.Patches
{
	/// <summary>
	/// Provides utility methods to override specific functionalities in the TilesetTypeUtility class through Harmony patches.
	/// </summary>
	/// <remarks>
	/// This class contains Harmony patches to intercept and customize the behavior of methods
	/// within the TilesetTypeUtility class. Each method applies a prefix patch to modify the
	/// output or bypass the original logic based on specific conditions.
	/// </remarks>
	public static class TilesetTypeUtility_Patch
	{
		/// Retrieves a ModTileset object corresponding to the given index.
		/// <param name="index">The index of the tileset to retrieve.</param>
		/// <returns>Returns the ModTileset if it exists in the custom tilesets or the missing tileset if the index is within the mod tileset range; otherwise, returns null.</returns>
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

		/// Retrieves a tileset by its index and attempts to populate the result with the corresponding tileset layers if available.
		/// <param name="index">The index of the tileset to retrieve.</param>
		/// <param name="__result">A reference to a PugMapTileset object that will be populated with the tileset layers if a matching tileset is found; otherwise, it will remain unchanged.</param>
		/// <returns>Returns false to prevent the original method execution when a valid tileset is retrieved; returns true to allow the original method execution if no matching tileset is found.</returns>
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

		/// Retrieves the texture for a specific layer and texture type within a tileset if it exists.
		/// <param name="tilesetIndex">The index of the tileset from which to retrieve the texture.</param>
		/// <param name="layerName">The name of the layer within the tileset to query for the texture.</param>
		/// <param name="textureType">The type of texture to retrieve, such as diffuse or normal map.</param>
		/// <param name="__result">A reference to a Texture2D object that will be populated with the retrieved texture if available; otherwise, it will remain null.</param>
		/// <returns>Returns false when the texture is successfully retrieved, preventing the original method from executing; returns true to allow the original method execution if no tileset is found or the texture is unavailable.</returns>
		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetTexture))]
		[HarmonyPrefix]

		public static bool GetTexture(int tilesetIndex, LayerName layerName, TextureType textureType, ref Texture2D __result)
		{
			ModTileset tileset = GetTileset(tilesetIndex);
			if (tileset != null)
			{
				__result = tileset.tilesetTextures.GetTexture(textureType);
				return false;
			}

			return true;
		}

		/// Retrieves the adaptive texture for the specified layer and texture type within a tileset if available.
		/// <param name="tilesetIndex">The index of the tileset to search for the adaptive texture.</param>
		/// <param name="layerName">The layer name within the tileset for which to retrieve the adaptive texture.</param>
		/// <param name="textureType">The type of texture to retrieve (e.g., diffuse, normal map, etc.).</param>
		/// <param name="__result">A reference to a Texture2D that will be populated with the adaptive texture if found; otherwise, it will remain null.</param>
		/// <returns>Returns false if the adaptive texture is successfully retrieved; returns true to allow the original method execution if the tileset is not found.</returns>
		[HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetAdaptiveTexture))]
		[HarmonyPrefix]
		public static bool GetAdaptiveTexture(int tilesetIndex, LayerName layerName, TextureType textureType, ref Texture2D __result)
		{
			ModTileset tileset = GetTileset(tilesetIndex);
			if (tileset != null)
			{
				__result = null;
				if (tileset.adaptiveTilesetTextures.ContainsKey(layerName))
				{
					__result = tileset.adaptiveTilesetTextures[layerName].GetTexture(textureType);
				}
				return false;
			}

			return true;
		}


		/// Retrieves the override material for a specified layer within a tileset if it is defined.
		/// <param name="tilesetIndex">The index of the tileset to search for the override material.</param>
		/// <param name="layerName">The layer name within the tileset for which to retrieve the override material.</param>
		/// <param name="__result">A reference to a Material that will be populated with the override material if found; otherwise, it will remain null.</param>
		/// <returns>Returns false if the override material is successfully retrieved; returns true to allow the original method execution if the tileset is not found.</returns>
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


		/// Retrieves the override material for a specific layer name within a tileset in the editor, if it is defined.
		/// <param name="tilesetIndex">The index of the tileset to search for the editor override material.</param>
		/// <param name="tileName">The layer name within the tileset for which to retrieve the override material.</param>
		/// <param name="__result">A reference to a Material that will be populated with the override material if found; otherwise, it will remain null.</param>
		/// <returns>Returns false if the override material is successfully retrieved or no override exists; returns true to continue the original method execution if the tileset is not found.</returns>
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


		/// Retrieves an override particle system for a specific layer name within a tileset, if it is defined.
		/// <param name="tilesetIndex">The index of the tileset to search for override particles.</param>
		/// <param name="tileName">The layer name within the tileset for which to retrieve the override particle system.</param>
		/// <param name="__result">A reference to a ParticleSystem that will be populated with the override particle system if found; otherwise, it will remain null.</param>
		/// <returns>Returns false if the override particle system is successfully retrieved or if no override exists; returns true to continue the original method execution if the tileset is not found.</returns>
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


		/// Gets the friendly name of a tileset based on its index.
		/// <param name="index">The index of the tileset whose friendly name is to be retrieved.</param>
		/// <param name="__result">A reference to a string where the friendly name of the tileset will be stored if found.</param>
		/// <returns>Returns false if the friendly name is successfully retrieved, otherwise returns true to continue the original method execution.</returns>
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