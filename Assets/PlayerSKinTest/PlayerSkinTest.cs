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
            ControlMappingModule.AddNewCategory("CoreLib");
            ControlMappingModule.AddKeyboardBind("TestActionList1", KeyboardKeyCode.A);
            ControlMappingModule.AddKeyboardBind("TestActionList2", KeyboardKeyCode.B);
            ControlMappingModule.AddKeyboardBind("TestActionList3", KeyboardKeyCode.C);
            ControlMappingModule.AddKeyboardBind("TestActionList4", KeyboardKeyCode.D);
        }

        public void Init() { }

        public void Shutdown() { }

        public void ModObjectLoaded(Object obj) { }

        public void Update() { }
    }
}