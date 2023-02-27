using System;
using System.Runtime.InteropServices;
using CoreLib.Submodules.ModEntity;
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
            ObjectCategoryTag itemTag = GetArmorTag(data.objectInfo.objectType);
            if (!data.objectInfo.tags.Contains(itemTag))
                data.objectInfo.tags.Add(itemTag);
            
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
                    return EntityModule.AddPlayerCustomization(new HelmSkin()
                    {
                        helmTexture = skinTexture.Value,
                        hairType = helmHairType.Value
                    });
                case ObjectType.BreastArmor:
                    return EntityModule.AddPlayerCustomization(new BreastArmorSkin()
                    {
                        breastTexture = skinTexture.Value,
                        shirtVisibility = shirtVisibility.Value
                    });
                case ObjectType.PantsArmor:
                    return EntityModule.AddPlayerCustomization(new PantsArmorSkin()
                    {
                        pantsTexture = skinTexture.Value,
                        pantsVisibility = pantsVisibility.Value
                    });
            }

            return 0;
        }

        private ObjectCategoryTag GetArmorTag(ObjectType type)
        {
            switch (type)
            {
                case ObjectType.Helm:
                    return ObjectCategoryTag.Helm;
                case ObjectType.BreastArmor:
                    return ObjectCategoryTag.BreastArmor;
                case ObjectType.PantsArmor:
                    return ObjectCategoryTag.PantsArmor;
            }

            return ObjectCategoryTag.None;
        }
    }
}