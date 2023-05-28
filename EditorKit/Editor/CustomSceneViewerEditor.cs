using System;
using System.Linq;
using EditorKit.Scripts;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CustomSceneViewer))]
[CanEditMultipleObjects]
public class CustomSceneViewerEditor : Editor
{
    public int currentIndex;
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        CustomSceneViewer viewer = (CustomSceneViewer)serializedObject.targetObject;

        if (viewer.dataTable == null) return;
        
        string[] maps = viewer.dataTable.scenes.Select(scene => scene.sceneName).ToArray();
        if (!string.IsNullOrEmpty(viewer.currentScene))
        {
            currentIndex = Array.IndexOf(maps, viewer.currentScene);
        }
        
        EditorGUI.BeginChangeCheck();

        currentIndex = EditorGUILayout.Popup("Scene", currentIndex, maps);

        if (EditorGUI.EndChangeCheck())
        {
            viewer.currentScene = maps[currentIndex];
        }

        if (GUILayout.Button("Show"))
        {
            viewer.Display();
        }
    }
}
