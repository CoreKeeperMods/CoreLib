using CoreLib;
using CoreLib.Submodule.Audio;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.ControlMapping;
using CoreLib.Submodule.Entity;
using CoreLib.Submodule.EquipmentSlot;
using CoreLib.Submodule.LootDrop;
using CoreLib.Submodule.TileSet;
using CoreLib.Submodule.UserInterface;
using PugMod;
using UnityEngine;
using Logger = CoreLib.Util.Logger;

namespace TestMod.Scripts
{
    public class TestMod : IMod
    {
        internal static Logger Log = new Logger("TestMod");
        
        public void EarlyInit()
        {
            Log.LogInfo($"Loading CoreLib Test Mod");
            CoreLibMod.LoadSubmodule(
                typeof(AudioModule),
                typeof(CommandModule),
                typeof(ControlMappingModule),
                typeof(EntityModule),
                typeof(EquipmentSlotModule),
                typeof(LootDropModule),
                typeof(TileSetModule),
                typeof(UserInterfaceModule)
            );
        }

        public void Init()
        {
        }

        public void Shutdown()
        {
        }

        public void ModObjectLoaded(Object obj)
        {
        }

        public void Update()
        {
        }
    }
}