using System;
using System.Collections.Generic;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;
using UnityEngine;

namespace CoreLib.Submodules.TileSet
{
    [CreateAssetMenu(fileName = "ModTileset", menuName = "New ModTileset", order = 2)]
    public class ModTileset : ScriptableObject
    {
        public string tilesetId;
        public PugMapTileset layers;
        
        public SerializableDictionary<LayerName, Material> overrideMaterials;
        
        public SerializableDictionary<LayerName, ParticleSystem> overrideParticles;
        
        public Texture2D tilesetTexture;
        public Texture2D tilesetEmissiveTexture;
        public SerializableDictionary<LayerName, Texture2D> adaptiveTilesetTextures;
    }
}