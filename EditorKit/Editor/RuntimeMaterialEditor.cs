using UnityEditor;
using CoreLib.Submodules.CustomEntity;
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
            runtimeMaterial.materialName = renderer.sharedMaterial.name;
        }
    }
}
