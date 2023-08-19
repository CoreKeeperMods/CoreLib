using System;
using CoreLib.Submodules.TileSet;
using PugTilemap;
using UnityEngine;

namespace CoreLib.Components
{
    public class ModTileCDAuthoring : ModCDAuthoringBase
    {
        public string tileset;
        public TileType tileType;
        
        public override bool Apply(MonoBehaviour data)
        {
            TileAuthoring tileCdAuthoring = gameObject.AddComponent<TileAuthoring>();
            tileCdAuthoring.tileset = TileSetModule.GetTilesetId(tileset);
            tileCdAuthoring.tileType = tileType;
            Destroy(this);
            return true;
        }
    }
}