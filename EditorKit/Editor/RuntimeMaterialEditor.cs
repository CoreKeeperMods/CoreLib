using System.Linq;
using CoreLib.Components;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RuntimeMaterial))]
public class RuntimeMaterialEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Use material name"))
        {
            RuntimeMaterial runtimeMaterial = (RuntimeMaterial)target;
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
            RuntimeMaterial runtimeMaterial = (RuntimeMaterial)target;
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
        
        if (GUILayout.Button("Update to V2"))
        {
            RuntimeMaterial runtimeMaterial = (RuntimeMaterial)target;

            RuntimeMaterialV2 newRuntimeMaterialV2 = runtimeMaterial.gameObject.AddComponent<RuntimeMaterialV2>();
            newRuntimeMaterialV2.materialName = runtimeMaterial.materialName;
            Object.DestroyImmediate(runtimeMaterial);
            EditorUtility.SetDirty(newRuntimeMaterialV2.gameObject);
        }
    }
}
