using CoreLib.Commands;
using CoreLib.JsonLoader;
using PugMod;
using UnityEngine;

namespace CoreLib.ModderTools
{
    public class CoreLibModderToolsMod : IMod
    {
        public const string NAME = "Core Lib Modder Tools";
        
        public void EarlyInit()
        {
            CoreLibMod.LoadModules(typeof(CommandsModule), typeof(JsonLoaderModule));
            CommandsModule.RegisterCommandHandler(typeof(DumpCommandHandler), NAME);
        }

        public void Init()
        {
        }

        public void Shutdown()
        {
            CommandsModule.UnregisterCommandHandler(typeof(DumpCommandHandler));
        }

        public void ModObjectLoaded(Object obj)
        {
        }

        public bool CanBeUnloaded()
        {
            return true;
        }

        public void Update()
        {
        }
    }
}