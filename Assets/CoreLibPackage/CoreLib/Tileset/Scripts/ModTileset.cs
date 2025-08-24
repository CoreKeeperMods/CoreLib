using Pug.UnityExtensions;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.TileSets
{
    /// <summary>
    /// Represents a custom tileset configuration within the CoreLib TileSets module.
    /// </summary>
    /// <remarks>
    /// This class is designed to support modifiable tilesets by allowing for the customization of associated textures,
    /// materials, and particle systems. It acts as a container for these layers and provides functionality for managing
    /// their override properties in a structured manner.
    /// </remarks>
    [CreateAssetMenu(fileName = "ModTileset", menuName = "CoreLib/Tileset/Mod Tileset")]
    public class ModTileset : ScriptableObject
    {
        /// Represents the unique identifier associated with a tileset. This property serves as a key to distinguish
        /// individual tilesets within the system, enabling their management, reference, and integration in various
        /// tileset operations and configurations.
        public string tilesetId;

        /// Represents the collection of layers defined within the tileset. This property defines the structure
        /// and configuration of the tileset, specifying the layout and properties for each layer that composes
        /// the tile-based map. It serves as a core component for customizing and managing the visual and functional
        /// aspects of layers in the tileset.
        public PugMapTileset layers;

        /// A dictionary that maps a specific layer name to a custom material used to override the default material
        /// for that layer in the tileset. This allows specifying alternative visual materials for each layer,
        /// enhancing the customization of the tile-based map rendering.
        public SerializableDictionary<LayerName, Material> overrideMaterials;

        /// A dictionary that associates each layer name with a specific particle system
        /// to be used as an override for default particle effects. This enables the customization
        /// of particle effects for individual layers of the tileset, allowing for enhanced visual
        /// and dynamic effects tailored to specific layer requirements.
        public SerializableDictionary<LayerName, ParticleSystem> overrideParticles;

        /// Represents a collection of textures associated with the tileset, categorized
        /// by their specific roles or types. This property provides access to textures,
        /// such as diffuse, normal, or other specialized textures, enabling detailed
        /// customization and rendering of tilemaps using this tileset.
        public MapWorkshopTilesetBank.TilesetTextures tilesetTextures;

        /// <summary>
        /// Represents the primary texture used for the tileset. This texture provides the visual representation of the tiles
        /// within the tileset and is a core component for rendering tilemaps based on this tileset.
        /// </summary>
        public Texture2D tilesetTexture;

        /// <summary>
        /// Represents the texture used for the emissive properties of a tileset.
        /// This texture defines areas on the tileset that emit light, providing an emissive effect in the rendered scene.
        /// </summary>
        public Texture2D tilesetEmissiveTexture;

        /// <summary>
        /// Stores adaptive tileset texture mappings for different layers.
        /// Allows for customizing textures based on layer names and supports retrieving textures
        /// dynamically for specific texture types within those layers.
        /// </summary>
        public SerializableDictionary<LayerName, MapWorkshopTilesetBank.TilesetTextures> adaptiveTilesetTextures;
    }
}