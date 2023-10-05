using System;
using System.IO;
using PugMod;
using UnityEditor;
using UnityEngine;

namespace CoreLib.Editor
{
    [CreateAssetMenu(fileName = "ChainBuilder", menuName = "New ChainBuilder", order = 2)]
    public class ChainBuilder : ScriptableObject
    {
        public ModBuilderSettings[] settings;

        [HideInInspector] public int buildIndex;
        [HideInInspector] public bool isBuilding;
        [HideInInspector] public int skipTicks;
    }

    [CustomEditor(typeof(ChainBuilder))]
    public class ChainBuilderEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint()
        {
            ChainBuilder builder = (ChainBuilder)target;
            return builder.isBuilding;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ChainBuilder builder = (ChainBuilder)target;

            if (!builder.isBuilding)
            {
                if (GUILayout.Button("Build All"))
                {
                    StartBuilding(builder);
                }
                
                return;
            }

            if (!EditorPrefs.HasKey(GAME_INSTALL_PATH_KEY))
            {
                Debug.LogError("You will need to choose the game install path in \"Find game files\" tab");
                StopBuilding(builder);
                return;
            }

            int currentIndex = builder.buildIndex + 1;
            if (currentIndex >= builder.settings.Length)
            {
                StopBuilding(builder);
                return;
            }

            float progress = (currentIndex + 1) / (float)builder.settings.Length;
            var mod = builder.settings[currentIndex];
            
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.ProgressBar(rect, progress, $"Building mod \"{mod.metadata.name}\"");

            builder.skipTicks--;
            if (builder.skipTicks > 0) return;

            
            builder.buildIndex = currentIndex;
            EditorUtility.SetDirty(builder);

            try
            {
                Build(builder, mod);
                builder.skipTicks = 3;
                EditorUtility.SetDirty(builder);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                StopBuilding(builder);
            }
        }

        private static void StartBuilding(ChainBuilder builder)
        {
            builder.isBuilding = true;
            builder.buildIndex = -1;
            builder.skipTicks = 3;
            EditorUtility.SetDirty(builder);
        }
        
        private static void StopBuilding(ChainBuilder builder)
        {
            builder.isBuilding = false;
            builder.buildIndex = -1;
            builder.skipTicks = 3;
            EditorUtility.SetDirty(builder);
        }

        private const string GAME_INSTALL_PATH_KEY = "PugMod/SDKWindow/GamePath";

        private void Build(ChainBuilder builder, ModBuilderSettings settings)
        {
            var path = EditorPrefs.GetString(GAME_INSTALL_PATH_KEY);

            if (Directory.Exists(Path.Combine(path, "CoreKeeper_Data")))
            {
                path = Path.Combine(path, "CoreKeeper_Data", "StreamingAssets", "Mods");
            }
            else if (Directory.Exists(Path.Combine(path, "Assets")))
            {
                // Installing to another Unity project
                path = Path.Combine(path, "Assets", "StreamingAssets", "Mods");
            }
            else
            {
                Debug.LogError($"Can't find game at {path}");
                return;
            }

            ModBuilder.BuildMod(settings, path, success =>
            {
                if (!success)
                {
                    Debug.LogError("Failed to export mod");
                    StopBuilding(builder);
                    return;
                }

                Debug.Log($"Mod {settings.metadata.name} successfully exported to {path}");
            });
        }
    }
}