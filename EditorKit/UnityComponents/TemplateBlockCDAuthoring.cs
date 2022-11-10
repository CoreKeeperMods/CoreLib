using CoreLib.Util;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using IntPtr = System.IntPtr;

namespace CoreLib.Components
{
    public struct TemplateBlockCD
    {
        public Color lightColor;
    }

    public class TemplateBlockCDAuthoring : ModCDAuthoringBase
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
        }

        public Sprite mainSprite;
        public Sprite floorSprite;
        public Sprite mainEmissiveSprite;
        public Sprite floorEmissiveSprite;
        public Sprite shadowSprite;
        public Color lightColor;
        public bool Apply(EntityMonoBehaviourData data)
        {
            return default(bool);
        }

        public bool Allocate()
        {
            return default(bool);
        }
    }
}