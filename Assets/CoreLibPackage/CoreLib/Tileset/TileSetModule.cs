using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Data;
using CoreLib.Submodule.Entity;
using CoreLib.Submodule.TileSet.Patch;
using CoreLib.Util;
using CoreLib.Util.Extension;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.TileSet
{
    /// Represents a module related to tile sets within the CoreLib framework.
    /// <remarks>
    /// The TileSetModule extends the BaseSubmodule and manages functionalities such as custom tilesets,
    /// tileset layers, and related configurations. This class includes internal properties and methods
    /// for handling tileset-specific data within the game framework.
    /// </remarks>
    public class TileSetModule : BaseSubmodule
    {
        #region PublicInterface
        
        public const string NAME = "Core Library - Tileset";
        
        internal static Logger log = new(NAME);
        
        /// Retrieves a tileset using a unique tileset identifier.
        /// <param name="itemID">A string representing the unique identifier of the tileset.</param>
        /// <returns>The tileset associated with the provided identifier.</returns>
        public static Tileset GetTilesetId(string itemID)
        {
            Instance.ThrowIfNotLoaded();

            return (Tileset)tilesetIDs.GetIndex(itemID);
        }

        [Obsolete("Manual registration is not needed anymore!")]
        public static void AddCustomTileset(ModTileset tileset)
        {
        }

        #endregion

        #region PrivateImplementation

        /// Represents the dependencies required by the <c>TileSetModule</c>.
        internal override Type[] Dependencies => new[] { typeof(EntityModule) };

        /// Provides a singleton instance of the <c>TileSetModule</c>.
        internal static TileSetModule Instance => CoreLibMod.GetModuleInstance<TileSetModule>();

        /// Maintains a collection of custom tilesets mapped to their corresponding Tileset identifiers.
        internal static Dictionary<Tileset, ModTileset> customTilesets = new();

        /// Stores a mapping of tileset layer names to their corresponding PugMapTileset instances.
        internal static Dictionary<string, PugMapTileset> tilesetLayers = new();

        /// Represents a collection of custom PugMapTileset layers added dynamically at runtime.
        internal static List<PugMapTileset> customLayers = new();

        /// Represents a default fallback ModTileset resource used as a placeholder for missing or undefined tilesets.
        internal static ModTileset missingTileset;

        /// Manages the mapping and retrieval of tileset IDs associated with item identifiers.
        internal static IdBindConfigFile tilesetIDs;

        /// Specifies the inclusive lower bound of the ID range allocated for custom mod tilesets.
        public const int MOD_TILESET_ID_RANGE_START = 100;

        /// Defines the exclusive upper bound of the ID range allocated for custom mod tilesets.
        public const int MOD_TILESET_ID_RANGE_END = 200;

        /// Overrides the base submodule's hook setup to apply specific patches required
        /// for Tileset functionality.
        internal override void SetHooks() => CoreLibMod.Patch(typeof(TilesetTypeUtilityPatch));

        /// Loads and initializes the TileSet module.
        internal override void Load()
        {
            base.Load();
            tilesetIDs = new IdBindConfigFile(CoreLibMod.modInfo, $"{CoreLibMod.CONFIG_FOLDER}CoreLib.TilesetID.cfg", MOD_TILESET_ID_RANGE_START, MOD_TILESET_ID_RANGE_END);
            InitTilesets();
            MaterialCrawler.MaterialSwapReady += SwapMaterials;
            
            
            foreach (var mod in DependentMods)
            {
                var tilesetList = mod.Assets.OfType<ModTileset>().ToList();
                
                foreach (var tileset in tilesetList)
                    AddCustomTilesetImpl(tileset);
                
                log.LogInfo($"Mod: {mod.Metadata.name} Found: {tilesetList.Count} Mod Tilesets");
            }
        }
        
        /// Adds a custom tileset to the tileset module.
        private static void AddCustomTilesetImpl(ModTileset tileset)
        {
            try
            {
                int itemIndex = tilesetIDs.GetNextId(tileset.tilesetId);
                Tileset tilesetID = (Tileset)itemIndex;

                if (tilesetLayers.TryGetValue(tileset.layers.name, out var layer))
                {
                    tileset.layers = layer;
                    log.LogInfo($"Replacing tileset {tileset.tilesetId} layers config with default layers {tileset.layers.name}");
                }
                else
                {
                    customLayers.Add(tileset.layers);
                }

                tileset.ValidateValues();

                customTilesets.Add(tilesetID, tileset);
                log.LogInfo($"Added tileset {tileset.tilesetId} as TilesetID: {tilesetID}!");
            }
            catch (Exception e)
            {
                log.LogError($"Failed to add tileset {tileset.tilesetId}:\n{e}");
            }
        }
        
        /// Updates the materials used in custom layers and their associated quad generators
        /// to align with the materials defined in the PrefabCrawler configuration.
        private static void SwapMaterials()
        {
            foreach (var layers in customLayers)
            {
                string materialName = layers.tilesetMaterial.name;
                if (MaterialCrawler.materials.TryGetValue(materialName, out var material))
                {
                    layers.tilesetMaterial = material;
                }
                
                foreach (var layer in layers.layers)
                {
                    if (layer.overrideMaterial == null) continue;
                    
                    materialName = layer.overrideMaterial.name;
                    if (MaterialCrawler.materials.TryGetValue(materialName, out var material1))
                    {
                        layer.overrideMaterial = material1;
                    }
                }
            }
        }

        /// Initializes tilesets by loading default tileset layers, associating custom tilesets,
        /// and setting up missing tileset configurations.
        private static void InitTilesets()
        {
            MapWorkshopTilesetBank vanillaBank = typeof(TilesetTypeUtility).GetValue<MapWorkshopTilesetBank>("tilesetBank");
            if (vanillaBank != null)
            {
                foreach (MapWorkshopTilesetBank.Tileset tileset in vanillaBank.tilesets)
                {
                    string layersName = tileset.layers.name;
                    if (tilesetLayers.ContainsKey(layersName)) continue;
                    tilesetLayers.Add(layersName, tileset.layers);

                    if (!layersName.Equals("tileset_extras")) continue;
                        
                    int railLayer = tileset.layers.layers.FindIndex(generator => generator.targetTile == TileType.rail);
                    if (railLayer > 0)
                        tileset.layers.layers[railLayer].onlyAdaptToOwnTileset = false;
                }
            }
            else
            {
                log.LogError("Failed to get default tileset layers!");
            }

            missingTileset = Mod.Assets.OfType<ModTileset>().ToList().Find(x => x.name == "MissingTileset");

            if (tilesetLayers.TryGetValue(missingTileset.layers.name, out var layer))
            {
                missingTileset.layers = layer;
            }
        }

        #endregion
    }
}