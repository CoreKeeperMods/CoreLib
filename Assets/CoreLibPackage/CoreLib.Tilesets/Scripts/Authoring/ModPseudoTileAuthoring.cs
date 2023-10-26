using CoreLib.Util.Atributes;
using PugConversion;
using PugTilemap;
using UnityEngine;

namespace CoreLib.TileSets.Components
{
    public class ModPseudoTileAuthoring : MonoBehaviour
    {
        [ModTileset]
        public string tileset;
        public TileType tileType;
    }

    public class ModPseudoTileConverter : SingleAuthoringComponentConverter<ModPseudoTileAuthoring>
    {
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