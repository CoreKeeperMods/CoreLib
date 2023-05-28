using System;
using System.Linq;
using CoreLibKit;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace EditorKit.Scripts
{
    public class TileView : MonoBehaviour
    {
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;

        public int3 pos;
        public int tileset;
        public TileType tileType;

        private static readonly int baseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int topRect = Shader.PropertyToID("_TopRect");
        private static readonly int sideRect = Shader.PropertyToID("_SideRect");
        private static readonly int topGenTex = Shader.PropertyToID("_TopGenTex");
        private static readonly int sideGenTex = Shader.PropertyToID("_SideGenTex");
        private static readonly int hasGenTex = Shader.PropertyToID("_HasGenTex");

        public void SetTile(int3 newPos, int newTileset, TileType newTileType)
        {
            pos = newPos;
            tileset = newTileset;
            tileType = newTileType;

            var posVec = new Vector3(pos.x, pos.y + 0.5f, pos.z);
            if (tileType == TileType.bigRoot ||
                tileType == TileType.smallStones ||
                tileType == TileType.floor ||
                tileType == TileType.groundSlime ||
                tileType == TileType.smallGrass ||
                tileType == TileType.dugUpGround ||
                tileType == TileType.rail ||
                tileType == TileType.rug ||
                tileType == TileType.debris ||
                tileType == TileType.debris2)
            {
                posVec.y = pos.y - 0.4f;
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            else
            {
                meshRenderer.shadowCastingMode = ShadowCastingMode.On;
            }

            transform.position = posVec;
        }

        public void UpdateVisuals()
        {
            var propertyBlock = CustomSceneViewer.Instance.propertyBlock;
            meshRenderer.GetPropertyBlock(propertyBlock);

            var tilesetObj = CustomSceneViewer.Instance.GetTileset(tileset);
            if (tilesetObj.tilesetTextures.texture == null) return;

            propertyBlock.SetTexture(baseMap, tilesetObj.tilesetTextures.texture);
            byte dirMask = CustomSceneViewer.Instance.GetAdjacentTileInfo(pos, this);
            int adaptiveTexMask = 0;

            var quads = tilesetObj.layers.layers.Where(generator => generator.targetTile == tileType);
            if (quads.Any())
            {
                byte seed = (byte)PugRandom.Range(0, 256, pos.x + pos.z);

                var topQuad = quads.FirstOrDefault(generator => generator.tileFaces.Contains(QuadGenerator.TileFace.TOP) ||
                                                                generator.tileFaces.Contains(QuadGenerator.TileFace.BOTTOM));
                if (topQuad != null)
                {
                    bool hasTopGenTex =
                        tilesetObj.adaptiveTilesetTextures.TryGetValue(topQuad.layerName, out MapWorkshopTilesetBank.TilesetTextures topTextures);
                    adaptiveTexMask |= hasTopGenTex ? 1 : 0;
                    if (hasTopGenTex)
                        propertyBlock.SetTexture(topGenTex, topTextures.texture);

                    Rect rect = GetSpriteRect(dirMask, topQuad, hasTopGenTex, seed);
                    propertyBlock.SetVector(topRect, rect.ToVector());
                }

                var sideQuad = quads.FirstOrDefault(generator => generator.tileFaces.Contains(QuadGenerator.TileFace.FRONT));
                if (sideQuad != null)
                {
                    bool hasTopGenTex =
                        tilesetObj.adaptiveTilesetTextures.TryGetValue(sideQuad.layerName, out MapWorkshopTilesetBank.TilesetTextures sideTextures);
                    adaptiveTexMask |= hasTopGenTex ? 2 : 0;
                    if (hasTopGenTex)
                        propertyBlock.SetTexture(sideGenTex, sideTextures.texture);

                    Rect rect = GetSpriteRect(dirMask, sideQuad, hasTopGenTex, seed);
                    propertyBlock.SetVector(sideRect, rect.ToVector());
                }
            }

            propertyBlock.SetInt(hasGenTex, adaptiveTexMask);
            meshRenderer.SetPropertyBlock(propertyBlock);

            if (tileType == TileType.wall)
            {
                meshFilter.mesh = CustomSceneViewer.Instance.skewedMesh;

                int3 prevPos = pos - new int3(0, 0, 1);
                if (CustomSceneViewer.Instance.TryGetTileAt(prevPos, out TileView tile))
                {
                    if (tile.tileType == TileType.wall)
                    {
                        meshFilter.mesh = CustomSceneViewer.Instance.skewedMesh2;
                    }
                }
            }
            else
            {
                meshFilter.mesh = CustomSceneViewer.Instance.boxMesh;
            }
        }

        private static Rect GetSpriteRect(byte dirMask, QuadGenerator topQuad, bool hasGenTex, byte seed)
        {
            if (topQuad.meshFillType == QuadGenerator.FillType.RandomFill)
            {
                return topQuad.allSpriteUVs[0].spriteUVS[UnityEngine.Random.Range(0, topQuad.allSpriteUVs[0].spriteUVS.Count)];
            }

            QuadGenerator.AdaptiveLUTType lutType = hasGenTex ? QuadGenerator.AdaptiveLUTType.GeneratedTexture : QuadGenerator.AdaptiveLUTType.NineWay;

            int mask = dirMask & topQuad.GetAdaptativeDirBitsAvailable(lutType)[0];
            Rect rect = topQuad.GetAdaptativeSpriteLookupTable(lutType)[0].GetSpriteCoordsForDirCombination((byte)mask, seed);

            if (rect.width > 0) return rect;

            rect = topQuad.GetAdaptativeSpriteLookupTable(lutType)[0].GetSpriteCoordsForDirCombination(0, seed);
            return rect;
        }
    }
}