using System.Linq;
using CoreLib.Components;
using UnityEditor;
using UnityEngine;

namespace EditorKit.Editor
{
    [CustomEditor(typeof(RuntimeMaterialV2))]
    public class RuntimeMaterialV2Editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            RuntimeMaterialV2 runtimeMaterial = (RuntimeMaterialV2)target;

            base.OnInspectorGUI();
            
            if (GUILayout.Button("Use material name"))
            {
                
                SpriteRenderer renderer = runtimeMaterial.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    runtimeMaterial.materialName = renderer.sharedMaterial.name;
                }
            
                ParticleSystemRenderer particleSystemRenderer = runtimeMaterial.GetComponent<ParticleSystemRenderer>();
                if (particleSystemRenderer != null)
                {
                    runtimeMaterial.materialName = particleSystemRenderer.sharedMaterial.name;
                }
                EditorUtility.SetDirty(runtimeMaterial);
            }
            
            if (GUILayout.Button("Reassign material")){
                SpriteRenderer renderer = runtimeMaterial.GetComponent<SpriteRenderer>();
                ParticleSystemRenderer particleSystemRenderer = runtimeMaterial.GetComponent<ParticleSystemRenderer>();

            

                string[] results = AssetDatabase.FindAssets($"t:material {runtimeMaterial.materialName}");
                if (results.Length > 0)
                {
                    string result = results.First();
                    string path = AssetDatabase.GUIDToAssetPath(result);
                    if (renderer != null)
                        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (particleSystemRenderer != null)
                        particleSystemRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);

                    EditorUtility.SetDirty(runtimeMaterial);
                }
                else
                {
                    Debug.Log("No matches found!");
                }
            }
        }
    }
}