using System;
using System.Collections.Generic;
using CoreLib.Data;
using CoreLib.Submodules.ModEntity;
using CoreLib.Submodules.TileSet.Patches;
using CoreLib.Util.Extensions;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;

namespace CoreLib.Submodules.TileSet
{
    public class TileSetModule : BaseSubmodule
    {
        #region PublicInterface

        /// <summary>
        /// Get Tileset from UNIQUE tileset id
        /// </summary>
        /// <param name="itemID">UNIQUE string tileset ID</param>
        public static Tileset GetTilesetId(string itemID)
        {
            Instance.ThrowIfNotLoaded();

            return (Tileset)tilesetIDs.GetIndex(itemID);
        }

        /// <summary>
        /// Add one or more custom tilesets. Prefab must be <see cref="MapWorkshopTilesetBank"/> with fields 'friendlyName' set to tileset ids
        /// </summary>
        /// <param name="tilesetPath">path to your prefab in asset bundle</param>
        /// <exception cref="ArgumentException">If provided prefab was not found</exception>
        /// <exception cref="InvalidOperationException">Throws if called too late</exception>
        public static void AddCustomTileset(ModTileset tileset)
        {
            Instance.ThrowIfNotLoaded();
            
            try
            {
                int itemIndex = tilesetIDs.GetNextId(tileset.tilesetId);
                Tileset tilesetID = (Tileset)itemIndex;

                if (tilesetLayers.ContainsKey(tileset.layers.name))
                {
                    tileset.layers = tilesetLayers[tileset.layers.name];
                    CoreLibMod.Log.LogDebug($"Replacing tileset {tileset.tilesetId} layers config with default layers {tileset.layers.name}");
                }
                else
                {
                    customLayers.Add(tileset.layers);
                }

                customTilesets.Add(tilesetID, tileset);
                CoreLibMod.Log.LogDebug($"Added tileset {tileset.tilesetId} as TilesetID: {tilesetID}!");
            }
            catch (Exception e)
            {
                CoreLibMod.Log.LogError($"Failed to add tileset {tileset.tilesetId}:\n{e}");
            }
        }

        #endregion

        #region PrivateImplementation

        internal override GameVersion Build => new GameVersion(0, 0, 0, 0, "");

        internal override Type[] Dependencies => new[] { typeof(EntityModule) };

        internal static TileSetModule Instance => CoreLibMod.GetModuleInstance<TileSetModule>();

        internal static Dictionary<Tileset, ModTileset> customTilesets =
            new Dictionary<Tileset, ModTileset>();

        internal static Dictionary<string, PugMapTileset> tilesetLayers = new Dictionary<string, PugMapTileset>();
        internal static List<PugMapTileset> customLayers = new List<PugMapTileset>();
        internal static ModTileset missingTileset;

        internal static IdBindConfigFile tilesetIDs;

        public const int modTilesetIdRangeStart = 100;
        public const int modTilesetIdRangeEnd = 200;

        internal override void SetHooks()
        {
            CoreLibMod.harmony.PatchAll(typeof(TilesetTypeUtility_Patch));
        }
        
        internal override void Load()
        {
            tilesetIDs = new IdBindConfigFile("CoreLib", "CoreLib.TilesetID", modTilesetIdRangeStart, modTilesetIdRangeEnd);
            InitTilesets();
            EntityModule.MaterialSwapReady += SwapMaterials;
        }

        private static void SwapMaterials()
        {
            foreach (PugMapTileset layers in customLayers)
            {
                string materialName = layers.tilesetMaterial.name;
                if (PrefabCrawler.materials.ContainsKey(materialName))
                {
                    layers.tilesetMaterial = PrefabCrawler.materials[materialName];
                }
                
                foreach (QuadGenerator layer in layers.layers)
                {
                    if (layer.overrideMaterial == null) continue;
                    
                    materialName = layer.overrideMaterial.name;
                    if (PrefabCrawler.materials.ContainsKey(materialName))
                    {
                        layer.overrideMaterial = PrefabCrawler.materials[materialName];
                    }
                }
            }
        }

        private static void InitTilesets()
        {
            MapWorkshopTilesetBank vanillaBank = typeof(TilesetTypeUtility).GetProperty<MapWorkshopTilesetBank>("tilesetBank");
            if (vanillaBank != null)
            {
                foreach (MapWorkshopTilesetBank.Tileset tileset in vanillaBank.tilesets)
                {
                    string layersName = tileset.layers.name;
                    PrefabCrawler.FindMaterialsInTilesetLayers(tileset.layers);
                    if (!tilesetLayers.ContainsKey(layersName))
                    {
                        tilesetLayers.Add(layersName, tileset.layers);

                        if (!layersName.Equals("tileset_extras")) continue;
                        
                        int railLayer = tileset.layers.layers.FindIndex(generator => generator.targetTile == TileType.rail);
                        if (railLayer > 0)
                            tileset.layers.layers[railLayer].onlyAdaptToOwnTileset = false;
                    }
                }
            }
            else
            {
                CoreLibMod.Log.LogError("Failed to get default tileset layers!");
            }

            missingTileset = CoreLibMod.assetBundle.LoadAsset<ModTileset>("Assets/CoreLib/Resources/Tileset/MissingTileset.asset");

            if (tilesetLayers.ContainsKey(missingTileset.layers.name))
            {
                missingTileset.layers = tilesetLayers[missingTileset.layers.name];
            }
        }

        #endregion
    }
}