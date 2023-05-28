using System;
using System.Collections.Generic;
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
    public class TileView : IEquatable<TileView>
    {
        public struct MeshData
        {
            public int instId;
            public float3 pos;
            public float4 topRect;
            public float4 sideRect;
            public ViewFlags viewFlags;
        };

        public int3 pos;
        public int tileset;
        public TileType tileType;
        
        public MeshData meshData;

        public bool useQuad;
        public Texture2D basetex;
        public Texture2D toptex;
        public Texture2D sidetex;

        public void SetTile(int3 newPos, int newTileset, TileType newTileType)
        {
            pos = newPos;
            tileset = newTileset;
            tileType = newTileType;
        }

        public TileGroup GetTileGroup()
        {
            return new TileGroup(useQuad, basetex, toptex, sidetex);
        }
        
        public void UpdateVisuals()
        {
            var tilesetObj = CustomSceneViewer.Instance.GetTileset(tileset);
            if (tilesetObj.tilesetTextures.texture == null) return;

            basetex = tilesetObj.tilesetTextures.texture;
            byte dirMask = CustomSceneViewer.Instance.GetAdjacentTileInfo(pos, this);
            meshData.viewFlags = ViewFlags.None;

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
                    if (hasTopGenTex)
                    {
                        meshData.viewFlags |= ViewFlags.AdaptiveTop;
                        toptex = topTextures.texture;
                    }

                    Rect rect = GetSpriteRect(dirMask, topQuad, hasTopGenTex, seed);
                    meshData.topRect = rect.ToFloat4();
                }

                var sideQuad = quads.FirstOrDefault(generator => generator.tileFaces.Contains(QuadGenerator.TileFace.FRONT));
                if (sideQuad != null)
                {
                    bool hasTopGenTex =
                        tilesetObj.adaptiveTilesetTextures.TryGetValue(sideQuad.layerName, out MapWorkshopTilesetBank.TilesetTextures sideTextures);
                    if (hasTopGenTex)
                    {
                        meshData.viewFlags |= ViewFlags.AdaptiveBottom;
                        sidetex = sideTextures.texture;
                    }

                    Rect rect = GetSpriteRect(dirMask, sideQuad, hasTopGenTex, seed);
                    meshData.sideRect = rect.ToFloat4();
                }
            }

            if (tileType == TileType.wall)
            {
                meshData.viewFlags |= ViewFlags.SkewBack;

                int3 prevPos = pos - new int3(0, 0, 1);
                if (CustomSceneViewer.Instance.TryGetTilesAt(prevPos, out List<TileView> tiles))
                {
                    if (tiles.Any(view => view.tileType == TileType.wall))
                    {
                        meshData.viewFlags |= ViewFlags.SkewFront;
                    }
                }
            }

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
                useQuad = true;
            }
            meshData.pos = new Vector3(pos.x, pos.y + 0.5f, pos.z);
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

        public bool Equals(TileView other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return pos.Equals(other.pos) && tileset == other.tileset && tileType == other.tileType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TileView)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(pos, tileset, (int)tileType);
        }

        public static bool operator ==(TileView left, TileView right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TileView left, TileView right)
        {
            return !Equals(left, right);
        }
    }
}