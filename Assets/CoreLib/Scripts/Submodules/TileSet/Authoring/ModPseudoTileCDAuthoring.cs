using CoreLib.Util.Atributes;
using PugConversion;
using PugTilemap;
using UnityEngine;

namespace CoreLib.Submodules.TileSet.Components
{
    public class ModPseudoTileCDAuthoring : MonoBehaviour
    {
        [ModTileset]
        public string tileset;
        public TileType tileType;
    }

    public class ModPseudoTileCDConverter : SingleAuthoringComponentConverter<ModPseudoTileCDAuthoring>
    {
        protected override void Convert(ModPseudoTileCDAuthoring authoring)
        {
            AddComponentData(new PseudoTileCD
            {
                tileset = (int)TileSetModule.GetTilesetId(authoring.tileset),
                tileType = authoring.tileType
            });
        }
    }
}