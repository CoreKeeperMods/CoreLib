using CoreLib.Util.Atributes;
using PugConversion;
using PugTilemap;
using UnityEngine;

namespace CoreLib.TileSets.Components
{
    /// <summary>
    /// ModTileAuthoring allows the user to define metadata for a specific tile
    /// by associating a tileset and tile type. The class is typically used
    /// in Unity's authoring workflows to define tile properties for conversion
    /// into runtime tile entities.
    /// </summary>
    /// <remarks>
    /// The tileset is identified using the ModTileset attribute. During conversion,
    /// this metadata is processed into TileCD, a component that holds the runtime
    /// representation of the tile data.
    /// </remarks>
    public class ModTileAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Represents the name or unique identifier of the tileset used in tile-based systems.
        /// </summary>
        /// <remarks>
        /// This variable is typically associated with a specific tileset configuration defined in the system.
        /// It can be annotated with <see cref="ModTilesetAttribute"/> for integration with custom editor tools.
        /// </remarks>
        [ModTileset]
        public string tileset;

        /// <summary>
        /// Represents a specific type of tile within a tileset.
        /// </summary>
        /// <remarks>
        /// This variable defines the classification or category of a tile
        /// and is utilized in conjunction with the associated tileset,
        /// allowing for structured and modular organization of tile properties.
        /// </remarks>
        public TileType tileType;
    }

    /// <summary>
    /// ModTileConverter handles the conversion of ModTileAuthoring components
    /// into TileCD components within the ECS world. This allows for the runtime
    /// representation of tile entities based on authoring data provided in Unity.
    /// </summary>
    /// <remarks>
    /// The class performs the translation of tileset and tile type information
    /// from ModTileAuthoring into an ECS-compatible format, ensuring the tile's
    /// metadata is properly represented and accessible during runtime.
    /// </remarks>
    public class ModTileConverter : SingleAuthoringComponentConverter<ModTileAuthoring>
    {
        /// <summary>
        /// Converts a ModTileAuthoring component into a TileCD component
        /// and adds the converted data to the ECS Entity.
        /// </summary>
        /// <param name="authoring">The ModTileAuthoring component containing the source data for the conversion.</param>
        protected override void Convert(ModTileAuthoring authoring)
        {
            AddComponentData(new TileCD
            {
                tileset = (int)TileSetModule.GetTilesetId(authoring.tileset),
                tileType = authoring.tileType
            });
        }
    }
}