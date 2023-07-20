using System;
using PugTilemap;
using UnityEngine;

namespace CoreLib.Components
{
    public class ModTileCDAuthoring : ModCDAuthoringBase
    {
        public String tileset;
        public TileType tileType;

        public override bool Apply(MonoBehaviour data)
        {
            TileAuthoring tileCdAuthoring = gameObject.AddComponent<TileAuthoring>();
            tileCdAuthoring.tileset = 0;//EntityModule.GetTilesetId(tileset);
            tileCdAuthoring.tileType = tileType;
            Destroy(this);
            return true;
        }
    }
}