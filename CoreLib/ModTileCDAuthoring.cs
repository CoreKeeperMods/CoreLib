using System.Runtime.InteropServices;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem;
using PugTilemap;
using UnityEngine;

namespace CoreLib.ModAuthoring;

public class ModTileCDAuthoring : MonoBehaviour
{
    public Il2CppReferenceField<String> tileset;
    private GCHandle tilesetHandle;
    public TileType tileType;

    public ModTileCDAuthoring(System.IntPtr ptr) : base(ptr) { }
    
    public void Awake()
    {
        tilesetHandle = GCHandle.Alloc(tileset.Value);
    }

}