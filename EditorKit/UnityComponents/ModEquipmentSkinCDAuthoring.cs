using System;
using UnityEngine;

namespace CoreLib.Components
{
    public class ModEquipmentSkinCDAuthoring : ModCDAuthoringBase
    {
        public Texture2D skinTexture;
        public HelmHairType helmHairType;
        public ShirtVisibility shirtVisibility;
        public PantsVisibility pantsVisibility;
        public bool Allocate()
        {
            return default(bool);
        }

        public bool Apply(EntityMonoBehaviourData data)
        {
            return default(bool);
        }
    }
}