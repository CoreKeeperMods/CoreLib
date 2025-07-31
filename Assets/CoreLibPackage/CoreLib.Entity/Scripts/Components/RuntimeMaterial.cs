using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Components
{
    [ExecuteAlways]
    public class RuntimeMaterial : ModCDAuthoringBase
    {
        public string materialName;
        public override bool Apply(MonoBehaviour data)
        {
            if (PrefabCrawler.materials.ContainsKey(materialName)) {
                if (gameObject.TryGetComponent(out SpriteRenderer spriteRenderer)) {
                    spriteRenderer.sharedMaterial = PrefabCrawler.materials[materialName];
                } else if (gameObject.TryGetComponent(out ParticleSystemRenderer particleSystemRenderer)) {
                    particleSystemRenderer.sharedMaterial = PrefabCrawler.materials[materialName];
                } else {
                    CoreLibMod.Log.LogInfo($"Error applying material {materialName}, found no valid target!");
                }
            } else {
                CoreLibMod.Log.LogInfo($"Error applying material {materialName}, such material is not found!");
            }

            return true;
        }
#if UNITY_EDITOR
        private void Awake()
        {
            if (string.IsNullOrEmpty(materialName))
            {
                UseMaterialName();
            }
        }
#endif

        public void ReassignMaterial()
        {
#if UNITY_EDITOR
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            ParticleSystemRenderer particleSystemRenderer = GetComponent<ParticleSystemRenderer>();


            string[] results = AssetDatabase.FindAssets($"t:material {materialName}");
            if (results.Length > 0)
            {
                string result = results.First();
                string path = AssetDatabase.GUIDToAssetPath(result);
                if (renderer != null)
                    renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (particleSystemRenderer != null)
                    particleSystemRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);

                EditorUtility.SetDirty(this);
            }
            else
            {
                Debug.Log("No matches found!");
            }
#endif
        }

        public void UseMaterialName()
        {
#if UNITY_EDITOR
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                materialName = renderer.sharedMaterial.name;
            }

            ParticleSystemRenderer particleSystemRenderer = GetComponent<ParticleSystemRenderer>();
            if (particleSystemRenderer != null)
            {
                materialName = particleSystemRenderer.sharedMaterial.name;
            }

            EditorUtility.SetDirty(this);
#endif
        }
    }
}