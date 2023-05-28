using System.Collections.Generic;
using PugTilemap;
using Unity.Mathematics;
using UnityEngine;

namespace EditorKit.Scripts
{
    public class MapChunkView : MonoBehaviour
    {
        public TileView TilePrefab;
        public CustomScenesDataTable.Map map;

        public void SetMap(CustomScenesDataTable.Map newMap)
        {
            map = newMap;
            
            foreach (PugMapLayerData layer in map.mapData.layers)
            {
                PugmapTileData tileData = layer.tileData;
                if (tileData.tileType == TileType.pit ||
                    tileData.tileType == TileType.litFloor ||
                    tileData.tileType == TileType.roofHole ||
                    tileData.tileType == TileType.ore ||
                    tileData.tileType == TileType.ancientCrystal ||
                    tileData.tileType == TileType.wallGrass) continue;
                
                foreach (PugMapLayerData.TileLayerChunk chunk in layer.tileDataChunks)
                {
                    PlaceTiles(chunk, transform, tileData.tilesetType, tileData.tileType);
                }
            }
        }
        
        
        private void PlaceTiles(PugMapLayerData.TileLayerChunk chunk, Transform root, int tileset, TileType tileType)
        {
            int starty = chunk.s / 100;
            int startx = chunk.s % 100;

            int endy = chunk.e / 100;
            int endx = chunk.e % 100;

            int yPos = 0;
            if (tileType == TileType.ground ||
                tileType == TileType.water ||
                tileType == TileType.bridge)
            {
                yPos = -1;
            }

            if (startx == endx)
            {
                for (int y = starty; y < endy; y++)
                {
                    CreateTile(root, new int3(startx, yPos, y), tileset, tileType);
                }
            }
            else if (starty == endy)
            {
                for (int x = startx; x < endx; x++)
                {
                    CreateTile(root, new int3(x, yPos, starty), tileset, tileType);
                }
            }
        }

        private void CreateTile(Transform root, int3 localPos, int tileset, TileType tileType)
        {
            TileView tile = Instantiate(TilePrefab, root.transform);
            int3 pos = localPos + map.localPosition.ToInt3();
            CustomSceneViewer.Instance.SetTileAt(pos, tile);

            tile.SetTile(pos, tileset, tileType);
        }

    }
}