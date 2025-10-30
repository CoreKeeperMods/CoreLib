using System;
using System.Collections.Generic;
using CoreLib.Data;
using CoreLib.Submodule.Entity;
using CoreLib.Submodule.Resource;
using CoreLib.Submodule.TileSets.Patches;
using CoreLib.Util.Extensions;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.TileSets
{
    /// <summary>
    /// Represents a module related to tile sets within the CoreLib framework.
    /// </summary>
    /// <remarks>
    /// The TileSetModule extends the BaseSubmodule and manages functionalities such as custom tilesets,
    /// tileset layers, and related configurations. This class includes internal properties and methods
    /// for handling tileset-specific data within the game framework.
    /// </remarks>
    public class TileSetModule : BaseSubmodule
    {
        #region PublicInterface
        
        public new const string Name = "Core Lib Tileset";
        /// <summary>
        /// Retrieves a tileset using a unique tileset identifier.
        /// </summary>
        /// <param name="itemID">A string representing the unique identifier of the tileset.</param>
        /// <returns>The tileset associated with the provided identifier.</returns>
        public static Tileset GetTilesetId(string itemID)
        {
            Instance.ThrowIfNotLoaded();

            return (Tileset)tilesetIDs.GetIndex(itemID);
        }

        /// <summary>
        /// Adds a custom tileset to the tileset module.
        /// </summary>
        /// <param name="tileset">The custom tileset to be added.</param>
        /// <exception cref="ArgumentException">Thrown when the provided tileset's prefab is not found.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the method is called after the module is loaded and can't accept additional tilesets.</exception>
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
                    Log.LogInfo($"Replacing tileset {tileset.tilesetId} layers config with default layers {tileset.layers.name}");
                }
                else
                {
                    customLayers.Add(tileset.layers);
                }

                customTilesets.Add(tilesetID, tileset);
                Log.LogInfo($"Added tileset {tileset.tilesetId} as TilesetID: {tilesetID}!");
            }
            catch (Exception e)
            {
                Log.LogError($"Failed to add tileset {tileset.tilesetId}:\n{e}");
            }
        }

        #endregion

        #region PrivateImplementation

        /// <summary>
        /// Represents the dependencies required by the <c>TileSetModule</c>.
        /// </summary>
        /// <remarks>
        /// The <c>Dependencies</c> property defines the submodules that the <c>TileSetModule</c> depends on
        /// for its functionality. It is used internally to ensure that the necessary modules,
        /// such as <c>EntityModule</c>, are properly loaded and available before initializing
        /// or executing operations in the <c>TileSetModule</c>.
        /// </remarks>
        internal override Type[] Dependencies => new[] { typeof(EntityModule) };

        /// <summary>
        /// Provides a singleton instance of the <c>TileSetModule</c>.
        /// </summary>
        /// <remarks>
        /// The <c>Instance</c> property serves as the single point of access to the <c>TileSetModule</c>.
        /// It is internally used to ensure proper initialization and facilitate operations
        /// within the module. Attempting to access features of the <c>TileSetModule</c>
        /// without utilizing this property may result in unintended behavior or errors.
        /// The property lazily retrieves or ensures the appropriate module instance
        /// through <c>CoreLibMod.GetModuleInstance&lt;TileSetModule&gt;()</c>.
        /// </remarks>
        internal static TileSetModule Instance => CoreLibMod.GetModuleInstance<TileSetModule>();

        /// <summary>
        /// Maintains a collection of custom tilesets mapped to their corresponding Tileset identifiers.
        /// </summary>
        /// <remarks>
        /// The <c>customTilesets</c> dictionary is used to store and manage associations between unique
        /// Tileset identifiers and their respective <c>ModTileset</c> instances. These custom tilesets
        /// can be dynamically added during runtime to extend or override default tileset configurations.
        /// This variable functions as a central registry for custom-modified tilesets, enabling efficient
        /// retrieval and integration within the tile management system.
        /// </remarks>
        internal static Dictionary<Tileset, ModTileset> customTilesets =
            new Dictionary<Tileset, ModTileset>();

        /// <summary>
        /// Stores a mapping of tileset layer names to their corresponding PugMapTileset instances.
        /// </summary>
        /// <remarks>
        /// The <c>tilesetLayers</c> dictionary is utilized to maintain a registry of predefined tileset layers,
        /// each identified by a unique string key representing the layer's name. This allows for efficient retrieval
        /// of layer configurations when initializing or modifying tilesets. It plays a critical role in managing
        /// the association of layer data with their respective tilesets, ensuring consistency and reuse across the system.
        /// This variable is populated during the initialization process and updated dynamically as needed.
        /// </remarks>
        internal static Dictionary<string, PugMapTileset> tilesetLayers = new Dictionary<string, PugMapTileset>();

        /// <summary>
        /// Represents a collection of custom PugMapTileset layers added dynamically at runtime.
        /// </summary>
        /// <remarks>
        /// This variable stores the custom tile layers used within the TileSetModule.
        /// It is designed to manage additional tileset layers that are registered through the AddCustomTileset method.
        /// The customLayers list plays a significant role in handling and rendering these dynamically defined layers.
        /// Its contents are utilized and iterated over in various internal processing tasks, including swapping materials and managing tileset data.
        /// </remarks>
        internal static List<PugMapTileset> customLayers = new List<PugMapTileset>();

        /// <summary>
        /// Represents a default fallback ModTileset resource used as a placeholder for missing or undefined tilesets.
        /// </summary>
        /// <remarks>
        /// This variable is initialized during the TileSetModule's setup process and is loaded from the specified assets folder.
        /// It serves as a critical resource to prevent errors or inconsistencies when a tileset cannot be found or is unavailable.
        /// The missingTileset is used within various contexts, including patching and layer management, as a reliable default reference.
        /// </remarks>
        internal static ModTileset missingTileset;

        /// <summary>
        /// Manages the mapping and retrieval of tileset IDs associated with item identifiers.
        /// </summary>
        /// <remarks>
        /// This variable serves as a configuration structure to bind and manage unique IDs
        /// for tilesets within a specified range. The IDs can be dynamically assigned and
        /// retrieved based on item identifiers while ensuring that the IDs for custom tilesets
        /// remain within the defined modifiable range. This helps to maintain consistency and
        /// prevent conflicts in tileset management.
        /// </remarks>
        internal static IdBindConfigFile tilesetIDs;

        /// <summary>
        /// Specifies the inclusive lower bound of the ID range allocated for custom mod tilesets.
        /// </summary>
        /// <remarks>
        /// This constant defines the starting value of the ID range for modded tilesets.
        /// IDs assigned to custom tilesets must fall within the range defined by
        /// <c>modTilesetIdRangeStart</c> and <c>modTilesetIdRangeEnd</c> (exclusive).
        /// This ensures that mod tileset IDs do not conflict with other system-defined IDs.
        /// </remarks>
        public const int modTilesetIdRangeStart = 100;

        /// <summary>
        /// Defines the exclusive upper bound of the ID range allocated for custom mod tilesets.
        /// </summary>
        /// <remarks>
        /// This constant specifies the end value of the ID range used for modded tilesets.
        /// The IDs in this range are used to uniquely identify custom tilesets within the system.
        /// Tileset IDs generated for mods must be in the range between <c>modTilesetIdRangeStart</c>
        /// and <c>modTilesetIdRangeEnd</c> (exclusive).
        /// </remarks>
        public const int modTilesetIdRangeEnd = 200;

        /// <summary>
        /// Overrides the base submodule's hook setup to apply specific patches required
        /// for Tileset functionality.
        /// </summary>
        /// <remarks>
        /// This method applies the appropriate patches to integrate Tileset-specific
        /// utility functionality into the CoreLib framework. It is invoked during
        /// the initialization process to ensure any necessary adjustments or extensions
        /// to Tileset behavior are properly configured.
        /// </remarks>
        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(TilesetTypeUtility_Patch));
        }

        /// <summary>
        /// Loads and initializes the TileSet module.
        /// </summary>
        /// <remarks>
        /// This method refreshes the module's resource bundles, loads the tileset ID configuration,
        /// initializes tilesets, and subscribes to events necessary for material swapping.
        /// It is invoked internally to set up the module's functionality.
        /// </remarks>
        internal override void Load()
        {
            ResourcesModule.RefreshModuleBundles();

            tilesetIDs = new IdBindConfigFile(CoreLibMod.ModInfo, $"{CoreLibMod.ConfigFolder}CoreLib.TilesetID.cfg", modTilesetIdRangeStart, modTilesetIdRangeEnd);
            InitTilesets();
            EntityModule.MaterialSwapReady += SwapMaterials;
        }

        /// <summary>
        /// Updates the materials used in custom layers and their associated quad generators
        /// to align with the materials defined in the PrefabCrawler configuration.
        /// </summary>
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

        /// <summary>
        /// Initializes tilesets by loading default tileset layers, associating custom tilesets,
        /// and setting up missing tileset configurations.
        /// </summary>
        private static void InitTilesets()
        {
            MapWorkshopTilesetBank vanillaBank = typeof(TilesetTypeUtility).GetValue<MapWorkshopTilesetBank>("tilesetBank");
            if (vanillaBank != null)
            {
                foreach (MapWorkshopTilesetBank.Tileset tileset in vanillaBank.tilesets)
                {
                    string layersName = tileset.layers.name;
                    PrefabCrawler.FindMaterialsInTilesetLayers(tileset.layers);
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
                Log.LogError("Failed to get default tileset layers!");
            }

            missingTileset = ResourcesModule.LoadAsset<ModTileset>("Assets/CoreLibPackage/CoreLib.Tilesets/Resources/MissingTileset");

            if (tilesetLayers.TryGetValue(missingTileset.layers.name, out var layer))
            {
                missingTileset.layers = layer;
            }
        }

        #endregion
    }
}