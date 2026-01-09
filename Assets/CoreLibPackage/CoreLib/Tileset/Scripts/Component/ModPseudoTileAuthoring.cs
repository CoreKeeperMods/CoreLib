using CoreLib.Submodule.TileSet.Attribute;
using Pug.Conversion;
using PugTilemap;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.TileSet.Component
{
    /// <summary>
    /// The ModPseudoTileAuthoring class represents an authoring component for specifying tile metadata
    /// such as the tileset and tile type that can be converted into runtime components.
    /// This class provides a structure for configuring tile-related data to be later used in the Unity
    /// entity conversion process for procedural or modifiable tile functionality.
    /// </summary>
    public class ModPseudoTileAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Represents the identifier for a tileset associated with this component.
        /// </summary>
        /// <remarks>
        /// The tileset is used for referencing specific collections of tiles in tilemap systems.
        /// This property is decorated with the <see cref="CoreLib.Submodule.TileSet.Attribute.ModTilesetAttribute"/>
        /// to designate that it refers to a moddable tileset.
        /// </remarks>
        [ModTileset]
        public string tileset;

        /// Represents the type of tile used in the tileset. This variable is part of the ModPseudoTileAuthoring
        /// component and is used in tile type conversion and association processes.
        public TileType tileType;
    }

    /// <summary>
    /// The ModPseudoTileConverter class is responsible for converting data from a ModPseudoTileAuthoring instance
    /// into a PseudoTileCD data structure and adding it as a component.
    /// This class extends the SingleAuthoringComponentConverter to handle the conversion of authoring components
    /// to runtime data during the Unity entity conversion process.
    /// </summary>
    public class ModPseudoTileConverter : SingleAuthoringComponentConverter<ModPseudoTileAuthoring>
    {
        /// Converts the properties of a ModPseudoTileAuthoring component to a PseudoTileCD data structure and adds it as a component.
        /// <param name="authoring">The source ModPseudoTileAuthoring instance containing the tileset and tileType data to convert.</param>
        protected override void Convert(ModPseudoTileAuthoring authoring)
        {
            AddComponentData(new PseudoTileCD
            {
                tileset = (int)TileSetModule.GetTilesetId(authoring.tileset),
                tileType = authoring.tileType
            });
        }
    }
}