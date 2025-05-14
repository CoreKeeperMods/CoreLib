using Pug.UnityExtensions;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;
using UnityEngine;

namespace CoreLib.TileSets
{
    [CreateAssetMenu(fileName = "ModTileset", menuName = "CoreLib/New ModTileset", order = 2)]
    public class ModTileset : ScriptableObject
    {
        public string tilesetId;
        public PugMapTileset layers;
        
        public SerializableDictionary<LayerName, Material> overrideMaterials;
        
        public SerializableDictionary<LayerName, ParticleSystem> overrideParticles;

        public MapWorkshopTilesetBank.TilesetTextures tilesetTextures;
        
        public Texture2D tilesetTexture;
        public Texture2D tilesetEmissiveTexture;
        public SerializableDictionary<LayerName, MapWorkshopTilesetBank.TilesetTextures> adaptiveTilesetTextures;
    }
}