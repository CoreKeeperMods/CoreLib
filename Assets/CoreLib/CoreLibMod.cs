using System.Collections;
using System.Reflection;
using CoreLib.Components;
using CoreLib.Submodules;
using CoreLib.Submodules.Equipment;
using HarmonyLib;
using PugMod;
using Unity.Entities;
using UnityEngine;

[assembly: AssemblyVersion("1.0.0.0")]

namespace CoreLib
{
    public class CoreLibMod : IMod
    {
        public static Logger Log = new Logger("Core Lib");
        
        public static Harmony harmony;
        public static VirtualAssetBundle assetBundle;

        public void EarlyInit()
        {
            harmony = new Harmony("CoreLib");
            API.Server.OnWorldCreated += WorldInitialize;
        }

        public void Init()
        {
        }

        public void Shutdown()
        {
        }

        public void ModObjectLoaded(Object obj)
        {
            assetBundle.Register(obj);
        }

        public bool CanBeUnloaded()
        {
            return false;
        }

        public void Update()
        {
        }

        public void WorldInitialize()
        {
        }
        
        private void OnObjectSpawned(Entity entity, EntityManager entitymanager, GameObject graphicalobject)
        {
        }

        public void MessageReceivedOnClient(int messageType, Entity entity, int value0, int value1)
        {
        }

        public void MessageReceivedOnServer(int messageType, Entity entity, int value0, int value1)
        {
        }
    }
}