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
        
        public new const string Name = "Core Library - Tileset";
        
        internal new static Logger Log = new(Name);
        
        /// <summary>
        /// Retrieves a tileset using a unique tileset identifier.
        /// </summary>
        /// <param name="itemID">A string representing the unique identifier of the tileset.</param>
        /// <returns>The tileset associated with the provided identifier.</returns>
        public static Tileset GetTilesetId(string itemID)
        {
            Instance.ThrowIfNotLoaded();

            return (Tileset)TilesetIDs.GetIndex(itemID);
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
                int itemIndex = TilesetIDs.GetNextId(tileset.tilesetId);
                Tileset tilesetID = (Tileset)itemIndex;

                if (TilesetLayers.TryGetValue(tileset.layers.name, out var layer))
                {
                    tileset.layers = layer;
                    Log.LogInfo($"Replacing tileset {tileset.tilesetId} layers config with default layers {tileset.layers.name}");
                }
                else
                {
                    CustomLayers.Add(tileset.layers);
                }

                CustomTilesets.Add(tilesetID, tileset);
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
        /// through <c>CoreLibMod.GetModuleInstance&lt;TileSetModule&gt;</c>.
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
        internal static Dictionary<Tileset, ModTileset> CustomTilesets = new();

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
        internal static Dictionary<string, PugMapTileset> TilesetLayers = new();

        /// <summary>
        /// Represents a collection of custom PugMapTileset layers added dynamically at runtime.
        /// </summary>
        /// <remarks>
        /// This variable stores the custom tile layers used within the TileSetModule.
        /// It is designed to manage additional tileset layers that are registered through the AddCustomTileset method.
        /// The customLayers list plays a significant role in handling and rendering these dynamically defined layers.
        /// Its contents are utilized and iterated over in various internal processing tasks, including swapping materials and managing tileset data.
        /// </remarks>
        internal static List<PugMapTileset> CustomLayers = new();

        /// <summary>
        /// Represents a default fallback ModTileset resource used as a placeholder for missing or undefined tilesets.
        /// </summary>
        /// <remarks>
        /// This variable is initialized during the TileSetModule's setup process and is loaded from the specified asset's folder.
        /// It serves as a critical resource to prevent errors or inconsistencies when a tileset cannot be found or is unavailable.
        /// The missingTileset is used within various contexts, including patching and layer management, as a reliable default reference.
        /// </remarks>
        internal static ModTileset MissingTileset;

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
        internal static IdBindConfigFile TilesetIDs;

        /// <summary>
        /// Specifies the inclusive lower bound of the ID range allocated for custom mod tilesets.
        /// </summary>
        /// <remarks>
        /// This constant defines the starting value of the ID range for modded tilesets.
        /// IDs assigned to custom tilesets must fall within the range defined by
        /// <c>modTilesetIdRangeStart</c> and <c>modTilesetIdRangeEnd</c> (exclusive).
        /// This ensures that mod tileset IDs do not conflict with other system-defined IDs.
        /// </remarks>
        public const int ModTilesetIdRangeStart = 100;

        /// <summary>
        /// Defines the exclusive upper bound of the ID range allocated for custom mod tilesets.
        /// </summary>
        /// <remarks>
        /// This constant specifies the end value of the ID range used for modded tilesets.
        /// The IDs in this range are used to uniquely identify custom tilesets within the system.
        /// Tileset IDs generated for mods must be in the range between <c>modTilesetIdRangeStart</c>
        /// and <c>modTilesetIdRangeEnd</c> (exclusive).
        /// </remarks>
        public const int ModTilesetIdRangeEnd = 200;

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
        internal override void SetHooks() => CoreLibMod.Patch(typeof(TilesetTypeUtilityPatch));

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
            base.Load();
            TilesetIDs = new IdBindConfigFile(CoreLibMod.ModInfo, $"{CoreLibMod.ConfigFolder}CoreLib.TilesetID.cfg", ModTilesetIdRangeStart, ModTilesetIdRangeEnd);
            InitTilesets();
            MaterialCrawler.MaterialSwapReady += SwapMaterials;
        }

        /// <summary>
        /// Updates the materials used in custom layers and their associated quad generators
        /// to align with the materials defined in the PrefabCrawler configuration.
        /// </summary>
        private static void SwapMaterials()
        {
            foreach (var layers in CustomLayers)
            {
                string materialName = layers.tilesetMaterial.name;
                if (MaterialCrawler.Materials.TryGetValue(materialName, out var material))
                {
                    layers.tilesetMaterial = material;
                }
                
                foreach (var layer in layers.layers)
                {
                    if (layer.overrideMaterial == null) continue;
                    
                    materialName = layer.overrideMaterial.name;
                    if (MaterialCrawler.Materials.TryGetValue(materialName, out var material1))
                    {
                        layer.overrideMaterial = material1;
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
                    if (TilesetLayers.ContainsKey(layersName)) continue;
                    TilesetLayers.Add(layersName, tileset.layers);

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

            MissingTileset = Mod.Assets.OfType<ModTileset>().ToList().Find(x => x.name == "MissingTileset");

            if (TilesetLayers.TryGetValue(MissingTileset.layers.name, out var layer))
            {
                MissingTileset.layers = layer;
            }
        }

        #endregion
    }
}