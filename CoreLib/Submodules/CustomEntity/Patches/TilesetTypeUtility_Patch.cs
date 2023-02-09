using CoreLib.Util.Extensions;
using HarmonyLib;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;
using UnityEngine;

namespace CoreLib.Submodules.CustomEntity.Patches;

public static class TilesetTypeUtility_Patch
{
	private static MapWorkshopTilesetBank.Tileset GetTileset(int index)
	{
		Tileset tilesetId = (Tileset)index;
		if (CustomEntityModule.customTilesets.ContainsKey(tilesetId))
		{
			return CustomEntityModule.customTilesets[tilesetId].obj;
		}
		/*
		if (index >= CustomEntityModule.modTilesetIdRangeStart)
		{
			return CustomEntityModule.missingTileset;
		}*/

		return null;
	}
	
	[HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetTileset))]
	[HarmonyPrefix]
    public static bool GetTileset(int index, ref PugMapTileset __result)
    {
	    MapWorkshopTilesetBank.Tileset tileset = GetTileset(index);
	    if (tileset != null)
	    {
		    __result = tileset.layers;
		    return false;
	    }

	    return true;
    }

    [HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetTexture))]
    [HarmonyPrefix]

    public static bool GetTexture(int tilesetIndex, LayerName layerName, ref Texture2D __result)
    {
	    MapWorkshopTilesetBank.Tileset tileset = GetTileset(tilesetIndex);
	    if (tileset != null)
	    {
		    GetTexture_Impl(out __result, tileset.tilesetTextures);
		    return false;
	    }

	    return true;
    }

    private static void GetTexture_Impl(out Texture2D __result, MapWorkshopTilesetBank.TilesetTextures textures)
    {
	    Season season = Manager.prefs.season;
	    if (season != Season.None &&
	        textures.seasonalTextures != null)
	    {
		    MapWorkshopTilesetBank.SeasonalTexture texture = textures.seasonalTextures.Find(texture => texture.season == season);
		    if (texture != null)
		    {
			    __result = texture.texture;
		    }
	    }

	    __result = textures.texture;
    }

    [HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetAdaptiveTexture))]
    [HarmonyPrefix]
    public static bool GetAdaptiveTexture(int tilesetIndex, LayerName layerName, ref Texture2D __result)
    {
	    
	    MapWorkshopTilesetBank.Tileset tileset = GetTileset(tilesetIndex);
	    if (tileset != null)
	    {
		    __result = null;
		    if (tileset.adaptiveTilesetTextures.ContainsKey(layerName))
		    {
			    GetTexture_Impl(out __result, tileset.adaptiveTilesetTextures[layerName]);
		    }
		    return false;
	    }

	    return true;
    }


    [HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetOverrideMaterial))]
    [HarmonyPrefix]
    public static bool GetOverrideMaterial(int tilesetIndex, LayerName layerName, ref Material __result)
    {
	    MapWorkshopTilesetBank.Tileset tileset = GetTileset(tilesetIndex);
	    if (tileset != null)
	    {
		    __result = null;
		    foreach (MapWorkshopTilesetBank.TileTypeOverrideMaterial overrideMaterial in tileset.overrideMaterials)
		    {
			    if (overrideMaterial.layerName == layerName)
			    {
				    __result = overrideMaterial.overrideMaterial;
				    break;
			    }
		    }
		    return false;
	    }

	    return true;
    }
    

    [HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetEditorOverrideMaterial))]
    [HarmonyPrefix]
    public static bool GetEditorOverrideMaterial(int tilesetIndex, LayerName tileName, ref Material __result)
    {
	    MapWorkshopTilesetBank.Tileset tileset = GetTileset(tilesetIndex);
	    if (tileset != null)
	    {
		    __result = null;
		    foreach (MapWorkshopTilesetBank.TileTypeOverrideMaterial overrideMaterial in tileset.overrideMaterials)
		    {
			    if (overrideMaterial.layerName == tileName)
			    {
				    __result = overrideMaterial.editorOverrideMaterial;
				    break;
			    }
		    }
		    return false;
	    }

	    return true;
    }

    
    [HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetOverrideParticles))]
    [HarmonyPrefix]
    public static bool GetOverrideParticles(int tilesetIndex, LayerName tileName, ref ParticleSystem __result)
    {
	    MapWorkshopTilesetBank.Tileset tileset = GetTileset(tilesetIndex);
	    if (tileset != null)
	    {
		    __result = null;
		    foreach (MapWorkshopTilesetBank.TileTypeOverrideParticles overrideMaterial in tileset.overrideParticles)
		    {
			    if (overrideMaterial.layerName == tileName)
			    {
				    __result = overrideMaterial.overrideParticlePrefab;
				    break;
			    }
		    }
		    return false;
	    }

	    return true;
    }

    
    [HarmonyPatch(typeof(TilesetTypeUtility), nameof(GetFriendlyName))]
    [HarmonyPrefix]
    public static bool GetFriendlyName(int index, ref string __result)
    {
	    MapWorkshopTilesetBank.Tileset tileset = GetTileset(index);
	    if (tileset != null)
	    {
		    __result = tileset.friendlyName;
		    return false;
	    }

	    return true;
    }
    
}