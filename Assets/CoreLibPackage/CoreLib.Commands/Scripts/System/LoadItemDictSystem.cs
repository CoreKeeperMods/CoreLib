using I2.Loc;
using PugMod;
using Unity.Entities;

namespace CoreLib.Commands
{
    /// <summary>
    /// The <c>LoadItemDictSystem</c> is a simulation system responsible for updating a
    /// dictionary of friendly item names if a localization source is available.
    /// </summary>
    /// <remarks>
    /// This system ensures that item-friendly names are loaded and stored based on specific
    /// localization sources. It operates within the <see cref="RunSimulationSystemGroup"/> and
    /// is executed in both the server and client simulation worlds as per the filtering criteria.
    /// </remarks>
    /// <example>
    /// This system requires the presence of a <see cref="PugDatabase.DatabaseBankCD"/> component
    /// to operate, ensuring it only runs in relevant worlds.
    /// </example>
    /// <seealso cref="Unity.Entities.WorldSystemFilterFlags"/>
    /// <seealso cref="RunSimulationSystemGroup"/>
    /// <seealso cref="PugSimulationSystemBase"/>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(RunSimulationSystemGroup))]
    public partial class LoadItemDictSystem : PugSimulationSystemBase
    {
        /// <summary>
        /// Indicates if the <c>LoadItemDictSystem</c> has been initialized.
        /// </summary>
        /// <remarks>
        /// This static variable is used to ensure that the initialization logic
        /// of the system runs only once. When set to <c>true</c>, the system has
        /// completed its initialization process, including loading relevant
        /// localization sources and preparing the item dictionary.
        /// The variable plays a crucial role in managing the enabling and
        /// execution state of the <c>LoadItemDictSystem</c>.
        /// </remarks>
        private static bool initialized;

        /// <summary>
        /// Overrides the initialization method to configure system-specific requirements and behavior during its creation.
        /// </summary>
        /// <remarks>
        /// This method ensures that the system is properly configured for updates by requiring the presence of specific components,
        /// such as <c>PugDatabase.DatabaseBankCD</c>. Additional configurations specific to runtime groups are also established here
        /// before invoking the base implementation.
        /// </remarks>
        protected override void OnCreate()
        {
            RequireForUpdate<PugDatabase.DatabaseBankCD>();
            UpdatesInRunGroup(); 
            base.OnCreate();
        }

        /// <summary>
        /// Executes the system's update logic during each simulation step and manages one-time initialization
        /// for item-friendly names based on localization sources.
        /// </summary>
        /// <remarks>
        /// This method ensures that item-friendly names are added only once by checking and leveraging localization sources.
        /// Once the initialization process is completed, the system is disabled to prevent redundant execution in subsequent updates.
        /// The method invokes the base update logic once extra functionality is processed.
        /// </remarks>
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

        /// <summary>
        /// Populates a dictionary with localized, user-friendly names for item entries sourced from a mod authoring lookup.
        /// </summary>
        /// <remarks>
        /// This method processes the object ID lookup provided by <c>ModAPIAuthoring</c>, searching for localized names
        /// for each item using the <c>LocalizationManager.TryGetTranslation</c> method. If a translation exists and is not
        /// already present in the <c>CommandsModule.friendlyNameDict</c>, it adds the corresponding name in lowercase
        /// along with its associated item ID to the dictionary. After completing the iteration, it logs the total number
        /// of successfully added entries.
        /// </remarks>
        private static void AddItemFrendlyNames()
        {
            int count = 0;
            var modAuthorings = (API.Authoring as ModAPIAuthoring).ObjectIDLookup;
            foreach (var pair in modAuthorings)
            {
                if (LocalizationManager.TryGetTranslation($"Items/{pair.Key}", out var translation,
                        overrideLanguage: "english"))
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