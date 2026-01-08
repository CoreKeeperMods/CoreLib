using CoreLib;
using CoreLib.Submodule.Audio;
using CoreLib.Submodule.ControlMapping;
using CoreLib.Submodule.Entity;
using CoreLib.Submodule.EquipmentSlot;
using CoreLib.Submodule.LootDrop;
using CoreLib.Submodule.Skin;
using CoreLib.Submodule.TileSet;
using PugMod;
using Rewired;
using UnityEngine;

namespace PlayerSkinTest
{
    public class PlayerSkinTest : IMod
    {
        public void EarlyInit()
        {
            CoreLibMod.LoadSubmodule(typeof(AudioModule), typeof(EquipmentSlotModule),
                typeof(LootDropModule), typeof(SkinModule), typeof(TileSetModule), typeof(EntityModule), typeof(ControlMappingModule));
            ControlMappingModule.AddKeyBind("TestKeyBind", KeyboardKeyCode.H);
            int catInt = ControlMappingModule.AddNewCategory("PlayerSkinTest");
            ControlMappingModule.AddKeyBind("TestActionList", KeyboardKeyCode.A, categoryID: catInt);
        }

        public void Init() { }

        public void Shutdown() { }

        public void ModObjectLoaded(Object obj) { }

        public void Update() { }
    }
    
    
}
