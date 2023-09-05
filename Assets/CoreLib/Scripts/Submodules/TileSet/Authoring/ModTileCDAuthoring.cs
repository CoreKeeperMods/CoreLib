using CoreLib.Scripts.Util.Atributes;
using CoreLib.Submodules.TileSet;
using PugConversion;
using PugTilemap;
using UnityEngine;

namespace CoreLib.Components
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