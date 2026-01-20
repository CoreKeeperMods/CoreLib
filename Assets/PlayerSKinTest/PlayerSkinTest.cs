using CoreLib;
using CoreLib.Submodule.Audio;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.ControlMapping;
using CoreLib.Submodule.Entity;
using CoreLib.Submodule.EquipmentSlot;
using CoreLib.Submodule.Localization;
using CoreLib.Submodule.LootDrop;
using CoreLib.Submodule.TileSet;
using CoreLib.Submodule.UserInterface;
using PugMod;
using UnityEngine;
namespace PlayerSkinTest
{
    public class PlayerSkinTest : IMod
    {
        public void EarlyInit()
        {
            CoreLibMod.LoadSubmodule(typeof(AudioModule), typeof(CommandModule), typeof(ControlMappingModule),
                typeof(EntityModule),typeof(EquipmentSlotModule), typeof(LocalizationModule), typeof(LootDropModule),
                typeof(TileSetModule), typeof(UserInterfaceModule));
        }

        public void Init() { }

        public void Shutdown() { }

        public void ModObjectLoaded(Object obj) { }

        public void Update() { }
    }
}