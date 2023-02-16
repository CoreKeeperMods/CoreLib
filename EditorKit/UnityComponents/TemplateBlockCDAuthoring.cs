using System;
using CoreLib.Util;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using IntPtr = System.IntPtr;
using String = System.String;

#pragma warning disable CS0649
namespace CoreLib.Components
{
    public struct TemplateBlockCD
    {
        public Color lightColor;
        public float3 verticalSpriteOffset;
        public float3 horizontalSpriteOffset;
        public float3 shadowOffset;
        public float3 prefabOffset;
        public int interactionId;
    }

    public class TemplateBlockCDAuthoring : ModCDAuthoringBase, IHasDefaultValue
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
        }

        public Sprite verticalSprite;
        public Sprite horizontalSprite;
        public Sprite verticalEmissiveSprite;
        public Sprite horizontalEmissiveSprite;
        public Sprite shadowSprite;
        public Color lightColor;
        public float3 verticalSpriteOffset;
        public float3 horizontalSpriteOffset;
        public float3 shadowOffset;
        public float3 prefabOffset;
        private int interactMethodIndex;
        public void InitDefaultValues()
        {
        }

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