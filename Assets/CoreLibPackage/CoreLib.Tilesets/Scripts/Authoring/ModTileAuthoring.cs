using CoreLib.Util.Atributes;
using PugConversion;
using PugTilemap;
using UnityEngine;

namespace CoreLib.TileSets.Components
{
    public class ModTileAuthoring : MonoBehaviour
    {
        [ModTileset]
        public string tileset;
        public TileType tileType;
    }
    
    public class ModTileConverter : SingleAuthoringComponentConverter<ModTileAuthoring>
    {
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