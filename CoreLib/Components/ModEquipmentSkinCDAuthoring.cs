using System;
using System.Runtime.InteropServices;
using CoreLib.Submodules.CustomEntity;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using UnityEngine;

namespace CoreLib.Components
{
    public class ModEquipmentSkinCDAuthoring : ModCDAuthoringBase
    {
        public Il2CppReferenceField<Texture2D> skinTexture;
        public Il2CppValueField<HelmHairType> helmHairType;
        public Il2CppValueField<ShirtVisibility> shirtVisibility;
        public Il2CppValueField<PantsVisibility> pantsVisibility;

        private GCHandle skinTextureHandle;

        public ModEquipmentSkinCDAuthoring(IntPtr ptr) : base(ptr) { }

        public override bool Allocate()
        {
            bool alloc = base.Allocate();
            if (alloc)
            {
                skinTextureHandle = GCHandle.Alloc(skinTexture.Value);
            }

            return alloc;
        }

        private void OnDestroy()
        {
            skinTextureHandle.Free();
        }

        public override bool Apply(EntityMonoBehaviourData data)
        {
            byte skinId = GetSkinForObjectType(data.objectInfo.objectType);

            if (skinId != 0)
            {
                var skinCdAuthoring = gameObject.AddComponent<EquipmentSkinCDAuthoring>();
                skinCdAuthoring.skin = skinId;
            }

            Destroy(this);
            return skinId != 0;
        }

        private byte GetSkinForObjectType(ObjectType type)
        {
            switch (type)
            {
                case ObjectType.Helm:
                    return CustomEntityModule.AddPlayerCustomization(new HelmSkin()
                    {
                        helmTexture = skinTexture.Value,
                        hairType = helmHairType.Value
                    });
                case ObjectType.BreastArmor:
                    return CustomEntityModule.AddPlayerCustomization(new BreastArmorSkin()
                    {
                        breastTexture = skinTexture.Value,
                        shirtVisibility = shirtVisibility.Value
                    });
                case ObjectType.PantsArmor:
                    return CustomEntityModule.AddPlayerCustomization(new PantsArmorSkin()
                    {
                        pantsTexture = skinTexture.Value,
                        pantsVisibility = pantsVisibility.Value
                    });
            }

            return 0;
        }
    }
}