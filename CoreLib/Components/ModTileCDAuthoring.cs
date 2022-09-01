using System.Runtime.InteropServices;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem;
using PugTilemap;

#if IL2CPP
namespace CoreLib.Submodules.CustomEntity
{

    public class ModTileCDAuthoring : ModCDAuthoringBase
    {
        public Il2CppReferenceField<String> tileset;
        private GCHandle tilesetHandle;
        public Il2CppValueField<TileType> tileType;

        public ModTileCDAuthoring(System.IntPtr ptr) : base(ptr) { }

        public override bool Allocate()
        {
            bool alloc = base.Allocate();
            if (alloc)
            {
                tilesetHandle = GCHandle.Alloc(tileset.Value);
            }
            return alloc;
        }

        private void OnDestroy()
        {
            tilesetHandle.Free();
        }

        public override bool Apply(EntityMonoBehaviourData data)
        {
            TileCDAuthoring tileCdAuthoring = gameObject.AddComponent<TileCDAuthoring>();
            tileCdAuthoring.tileset = CustomEntityModule.GetTilesetId(tileset.Value);
            tileCdAuthoring.tileType = tileType.Value;
            Destroy(this);
            return true;
        }
    }
}
#endif