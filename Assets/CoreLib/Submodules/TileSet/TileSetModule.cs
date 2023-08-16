﻿using System;
using System.Collections.Generic;
using System.Reflection;
using CoreLib.Extensions;
using CoreLib.Submodules.TileSet.Patches;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;

namespace CoreLib.Submodules.TileSet
{
    [CoreLibSubmodule]
    public static class TileSetModule
    {
        #region PublicInterface

        /// <summary>
        /// Get Tileset from UNIQUE tileset id
        /// </summary>
        /// <param name="itemID">UNIQUE string tileset ID</param>
        public static Tileset GetTilesetId(string itemID)
        {
            ThrowIfNotLoaded();

            return (Tileset)tilesetIDs.GetIndex(itemID);
        }
        
        /// <summary>
        /// Add one or more custom tilesets. Prefab must be <see cref="MapWorkshopTilesetBank"/> with fields 'friendlyName' set to tileset ids
        /// </summary>
        /// <param name="tilesetPath">path to your prefab in asset bundle</param>
        /// <exception cref="ArgumentException">If provided prefab was not found</exception>
        /// <exception cref="InvalidOperationException">Throws if called too late</exception>
        public static void AddCustomTileset(MapWorkshopTilesetBank tilesetBank)
        {
            ThrowIfNotLoaded();

            foreach (MapWorkshopTilesetBank.Tileset tileset in tilesetBank.tilesets)
            {
                try
                {
                    int itemIndex = tilesetIDs.GetNextId(tileset.friendlyName);
                    Tileset tilesetID = (Tileset)itemIndex;

                    if (tilesetLayers.ContainsKey(tileset.layers.name))
                    {
                        tileset.layers = tilesetLayers[tileset.layers.name];
                        CoreLibMod.Log.LogDebug($"Replacing tileset {tileset.friendlyName} layers config with default layers {tileset.layers.name}");
                    }

                    customTilesets.Add(tilesetID, tileset);
                    CoreLibMod.Log.LogDebug($"Added tileset {tileset.friendlyName} as TilesetID: {tilesetID}!");
                }
                catch (Exception e)
                {
                    CoreLibMod.Log.LogError($"Failed to add tileset {tileset.friendlyName}:\n{e}");
                }
            }
        }
        
        
        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded
        {
            get => _loaded;
            internal set => _loaded = value;
        }

        #endregion

        #region PrivateImplementation

        private static bool _loaded;
        
        internal static Dictionary<Tileset, MapWorkshopTilesetBank.Tileset> customTilesets =
            new Dictionary<Tileset, MapWorkshopTilesetBank.Tileset>();
        
        internal static Dictionary<string, PugMapTileset> tilesetLayers = new Dictionary<string, PugMapTileset>();
        internal static MapWorkshopTilesetBank.Tileset missingTileset;
        
        internal static IdBindConfigFile tilesetIDs;
        
        public const int modTilesetIdRangeStart = 100;
        public const int modTilesetIdRangeEnd = 200;

        internal static void ThrowIfNotLoaded()
        {
            if (!Loaded)
            {
                Type submoduleType = MethodBase.GetCurrentMethod().DeclaringType;
                string message = $"{submoduleType.Name} is not loaded. Please use [{nameof(CoreLibSubmoduleDependency)}(nameof({submoduleType.Name})]";
                throw new InvalidOperationException(message);
            }
        }
        
        [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks()
        {
            CoreLibMod.harmony.PatchAll(typeof(TilesetTypeUtility_Patch));
        }

        [CoreLibSubmoduleInit(Stage = InitStage.Load)]
        internal static void Load()
        {
            tilesetIDs = new IdBindConfigFile($"CoreLib/CoreLib.TilesetID.cfg", modTilesetIdRangeStart, modTilesetIdRangeEnd);
            InitTilesets();
        }
        
        private static void InitTilesets()
        {
            MapWorkshopTilesetBank vanillaBank = typeof(TilesetTypeUtility).GetProperty<MapWorkshopTilesetBank>("tilesetBank");
            if (vanillaBank != null)
            {
                foreach (MapWorkshopTilesetBank.Tileset tileset in vanillaBank.tilesets)
                {
                    string layersName = tileset.layers.name;
                    if (!tilesetLayers.ContainsKey(layersName))
                    {
                        tilesetLayers.Add(layersName, tileset.layers);
                    }
                }
            }
            else
            {
                CoreLibMod.Log.LogError("Failed to get default tileset layers!");
            }

            MapWorkshopTilesetBank tilesetBank = CoreLibMod.assetBundle.Get<MapWorkshopTilesetBank>("MissingTileset");

            missingTileset = tilesetBank.tilesets[0];

            if (tilesetLayers.ContainsKey(missingTileset.layers.name))
            {
                missingTileset.layers = tilesetLayers[missingTileset.layers.name];
            }
        }

        #endregion
    }
}