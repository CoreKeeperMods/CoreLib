using CoreLib.Util.Atributes;
using PugConversion;
using PugTilemap;
using UnityEngine;

namespace CoreLib.Submodules.TileSet.Components
{
    public class ModTileCDAuthoring : MonoBehaviour
    {
        [ModTileset]
        public string tileset;
        public TileType tileType;
    }
    
    public class ModTileCDConverter : SingleAuthoringComponentConverter<ModTileCDAuthoring>
    {
        protected override void Convert(ModTileCDAuthoring authoring)
        {
            AddComponentData(new TileCD
            {
                tileset = (int)TileSetModule.GetTilesetId(authoring.tileset),
                tileType = authoring.tileType
            });
        }
    }
}