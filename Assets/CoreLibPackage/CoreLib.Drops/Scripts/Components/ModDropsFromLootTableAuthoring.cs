using PugConversion;
using UnityEngine;

namespace CoreLib.Drops.Components
{
    public class ModDropsFromLootTableAuthoring : MonoBehaviour
    {
        public string lootTableId;
    }
    
    public class ModDropsFromLootTableAuthoringConverter : SingleAuthoringComponentConverter<ModDropsFromLootTableAuthoring>
    {
        protected override void Convert(ModDropsFromLootTableAuthoring authoring)
        {
            AddComponentData(new DropsLootFromLootTableCD
            {
                lootTableID = DropTablesModule.GetLootTableID(authoring.lootTableId)
            });
        }
    }
}