using System;
using System.Collections.Generic;
using Pug.UnityExtensions;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.TileSet
{
    /// Represents a custom tileset configuration within the CoreLib TileSets module.
    [CreateAssetMenu(menuName = "CoreLib/Tileset/Mod Tileset", fileName = "ModTileset")]
    public class ModTileset : ScriptableObject
    {
        /// Represents the unique identifier associated with a tileset. 
        public string tilesetId;
        
        public Sprite icon;

        /// Reference to tileset target layer asset. 
        public PugMapTileset layers;
        
        /// A dictionary that maps a specific layer name to a custom material used to override the default material
        [ArrayElementTitle("layerName")]
        public List<MapWorkshopTilesetBank.TileTypeOverrideMaterial> overrideMaterials;

        /// A dictionary that associates each layer name with a specific particle system
        [ArrayElementTitle("layerName")]
        public List<MapWorkshopTilesetBank.TileTypeOverrideParticles> overrideParticles;

        /// Represents a collection of textures associated with the tileset, categorized
        /// by their specific roles or types. This property provides access to textures,
        /// such as diffuse, normal, or other specialized textures, enabling detailed
        /// customization and rendering of tilemaps using this tileset.
        public MapWorkshopTilesetBank.TilesetTextures tilesetTextures;
        
        /// Represents the primary texture used for the tileset. This texture provides the visual representation of the tiles
        /// within the tileset and is a core component for rendering tilemaps based on this tileset.
        public Texture2D tilesetTexture;

        /// Represents the texture used for the emissive properties of a tileset.
        /// This texture defines areas on the tileset that emit light, providing an emissive effect in the rendered scene.
        public Texture2D tilesetEmissiveTexture;

        /// Stores adaptive tileset texture mappings for different layers.
        /// Allows for customizing textures based on layer names and supports retrieving textures
        /// dynamically for specific texture types within those layers.
        public SerializableDictionary<LayerName, MapWorkshopTilesetBank.TilesetTextures> adaptiveTilesetTextures;

        internal void ValidateValues()
        {
            overrideMaterials ??= new();
            
            overrideParticles ??= new(); 
            tilesetTextures ??= new(); 
            adaptiveTilesetTextures ??= new(); 
        }
        
    }
}