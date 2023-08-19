using System;
using CoreLib.Submodules.TileSet;
using PugTilemap;
using UnityEngine;

namespace CoreLib.Components
{
    public class ModPseudoTileCDAuthoring : ModCDAuthoringBase
    {
        public string tileset;
        public TileType tileType;

        public override bool Apply(MonoBehaviour data)
        {
            PseudoTileAuthoring tileCdAuthoring = gameObject.AddComponent<PseudoTileAuthoring>();
            tileCdAuthoring.tileset = TileSetModule.GetTilesetId(tileset);
            tileCdAuthoring.tileType = tileType;
            Destroy(this);
            return true;
        }
    }
}