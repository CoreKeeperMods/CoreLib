using I2.Loc;
using PugMod;
using Unity.Entities;

namespace CoreLib.Commands
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(RunSimulationSystemGroup))]
    public partial class LoadItemDictSystem : PugSimulationSystemBase
    {
        private static bool initialized;

        protected override void OnCreate()
        {
            RequireForUpdate<PugDatabase.DatabaseBankCD>();
            UpdatesInRunGroup(); 
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            if (initialized)
            {
                base.OnUpdate();
                return;
            }

            if (LocalizationManager.Sources.Count > 0)
            {
                AddItemFrendlyNames();
            }

            initialized = true;
            Enabled = false;

            base.OnUpdate();
        }

        private static void AddItemFrendlyNames()
        {
            int count = 0;
            var modAuthorings = (API.Authoring as ModAPIAuthoring).ObjectIDLookup;
            foreach (var pair in modAuthorings)
            {
                if (LocalizationManager.TryGetTranslation($"Items/{pair.Key}", out var translation, overrideLanguage: "english"))
                {
                    if (!CommandsModule.friendlyNameDict.ContainsKey(translation.ToLower()))
                    {
                        CommandsModule.friendlyNameDict.Add(translation.ToLower(), pair.Value);
                        count++;
                    }
                }
            }

            CoreLibMod.Log.LogInfo($"Got {count} friendly name entries!");
        }
    }
}