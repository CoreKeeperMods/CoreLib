using System;
using System.Collections.Generic;
using CoreLib;
using UnityEngine;

namespace CoreLib.Submodules.CustomEntity
{
    public class RuntimeMaterial : MonoBehaviour
    {
        public static List<RuntimeMaterial> applyQueue = new List<RuntimeMaterial>();

        public string materialName;

        private SpriteRenderer spriteRenderer;


        public RuntimeMaterial(IntPtr ptr) : base(ptr) { }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            materialName = spriteRenderer.sharedMaterial.name;

            if (MaterialCrawler.isReady)
            {
                Apply(this);
            }
            else
            {
                applyQueue.Add(this);
            }
        }

        public static void Apply(RuntimeMaterial material)
        {
            if (MaterialCrawler.materials.ContainsKey(material.materialName))
            {
                material.spriteRenderer.sharedMaterial = MaterialCrawler.materials[material.materialName];
            }
            else
            {
                CoreLibPlugin.Logger.LogInfo($"Error applying material {material.materialName}");
            }
        }
    }
}