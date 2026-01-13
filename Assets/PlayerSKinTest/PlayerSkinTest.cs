using CoreLib;
using CoreLib.Submodule.Audio;
using CoreLib.Submodule.ControlMapping;
using CoreLib.Submodule.Entity;
using CoreLib.Submodule.EquipmentSlot;
using CoreLib.Submodule.LootDrop;
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
                typeof(LootDropModule), typeof(TileSetModule), typeof(EntityModule), typeof(ControlMappingModule));
            ControlMappingModule.AddKeyboardBind("TestKeyBind", KeyboardKeyCode.H);
            int catInt = ControlMappingModule.AddNewCategory("PlayerSkinTest");
            ControlMappingModule.AddKeyboardBind("TestActionList", KeyboardKeyCode.A, categoryId: catInt);
            ControlMappingModule.AddKeyboardBind("TestActionList2", KeyboardKeyCode.B, categoryId: catInt);
            ControlMappingModule.AddKeyboardBind("TestActionList3", KeyboardKeyCode.C, categoryId: catInt);
        }

        public void Init() { }

        public void Shutdown() { }

        public void ModObjectLoaded(Object obj) { }

        public void Update() { }
    }
}