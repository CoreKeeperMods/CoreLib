using System;

namespace CoreLib.Components
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