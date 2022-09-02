using System;

namespace CoreLib.Submodules.CustomEntity
{
    public class EntityPrefabOverride : ModCDAuthoringBase
    {
        public ObjectID sourceEntity;
        public bool Apply(EntityMonoBehaviourData data)
        {
            return default(bool);
        }
    }
}