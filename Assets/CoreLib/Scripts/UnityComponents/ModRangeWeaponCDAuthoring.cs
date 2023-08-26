using UnityEngine;
using String = System.String;

namespace CoreLib.Components
{
    public class ModRangeWeaponCDAuthoring : ModCDAuthoringBase
    {
        public String projectileID;
        public float spawnOffsetDistance;

        public override bool Apply(MonoBehaviour data)
        {
            RangeWeaponAuthoring rangeWeaponCdAuthoring = gameObject.AddComponent<RangeWeaponAuthoring>();
            rangeWeaponCdAuthoring.projectileID = 0;//EntityModule.GetObjectId(projectileID.Value);
            rangeWeaponCdAuthoring.spawnOffsetDistance = spawnOffsetDistance;
            Destroy(this);
            return true;
        }
    }
}