using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig.Helpers;
using PugMod;
using PugMod.ModIO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using ModIO;
using NaughtyAttributes;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    [Serializable]
    public class ModState
    {
        [HideInInspector] public string name;
        
        public ModSettings mod;
        
        [OnValueChanged("ShouldBuild_OnChange"), AllowNesting]
        public bool shouldBuild;

        [OnValueChanged("Version_OnChange"), AllowNesting]
        public string version;

        [HideInInspector]
        public string[] originalTags = Array.Empty<string>();
        
        [ModIOTag(inverted: true)] public string otherTags;
        
        [ModIOTag] public string gameVersionTags;
        
        public event Action OnSelectionChanged;
        
        internal void ShouldBuild_OnChange()
        {
            name = mod?.modSettings == null ? null : $"{mod.modSettings.metadata.name}{(shouldBuild ? "" : " (Disabled)")}";
            OnSelectionChanged?.Invoke();
        }

        internal void Version_OnChange()
        {
            string[] parts = version.Split('.');
            Array.Resize(ref parts, 3);
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i])) parts[i] = "0";
                else
                {
                    var regex = new Regex(@"^0+(?=\d)");
                    parts[i] = regex.Replace(parts[i], "");
                    parts[i] = string.IsNullOrEmpty(parts[i]) ? "0" : parts[i];
                }
                
            }
            version = string.Join('.', parts);
        }
    }

    [CreateAssetMenu(menuName = "CoreLib/Editor/Chain Builder", fileName = "Chain Builder")]
    public class ChainBuilder : ScriptableObject
    {
        private ModState[] _previousMods;
        public ModState[] mods;

        [OnValueChanged("BuildAll_OnChange"), AllowNesting]
        public bool buildAll;
        
        [ModIOTag] public string allGameVersions;
        
        [HideInInspector] public string actionPrefix = "Building";
        [HideInInspector] public int buildIndex;
        [HideInInspector] public bool isBuilding;
        
        internal void BuildAll_OnChange()
        {
            foreach (var modItem in mods)
            {
                modItem.shouldBuild = buildAll;
                modItem.name = modItem.mod?.modSettings == null ? null : $"{modItem.mod.modSettings.metadata.name}{(modItem.shouldBuild ? "" : " (Disabled)")}";
            }
        }
        
        private void UpdateBuildAll()
        {
            buildAll = Array.TrueForAll(mods, mod => mod.shouldBuild);
        }
        
        private void OnValidate()
        {
            if (mods == _previousMods) return;
            foreach (var item in mods)
            {
                if (item != null)
                {
                    item.OnSelectionChanged += UpdateBuildAll; // Subscribe
                }
            }
            _previousMods = mods;
            UpdateBuildAll();
        }
    }

    [CustomEditor(typeof(ChainBuilder))]
    public class ChainBuilderEditor : UnityEditor.Editor
    {
        private const string GameInstallPathKey = "PugMod/SDKWindow/GamePath";

        private static EditorCoroutine _coroutine;

        public override bool RequiresConstantRepaint()
        {
            var builder = (ChainBuilder)target;
            return builder.isBuilding;
        }
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var builder = (ChainBuilder)target;
            
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            using (new EditorGUI.DisabledScope(builder.isBuilding))
            {
                if (GUILayout.Button(new GUIContent("Sync All Game Versions",
                            EditorGUIUtility.IconContent("d_Refresh").image), 
                        GUILayout.Height(25)))
                    StartAction(builder, "Syncing");
                EditorGUILayout.Separator();
                if (GUILayout.Button(new GUIContent("Fetch Mod.io Data",
                            EditorGUIUtility.IconContent("Import-Available").image), 
                        GUILayout.Height(25)))
                    StartAction(builder, "Fetching");
                EditorGUILayout.Separator();
                using (new EditorGUI.DisabledScope(builder.mods is null || builder.mods.Length == 0 || builder.mods.All(mod => !mod.shouldBuild)))
                {
                    if (GUILayout.Button(new GUIContent($"Build {(builder.mods is not null && builder.mods.All(mod => mod.shouldBuild) ? "All " : "")}Mods",
                                EditorGUIUtility.IconContent("Settings").image),
                            GUILayout.Height(25)))
                        StartAction(builder, "Building");
                }
            }

            if (!builder.isBuilding) return;
            
            if (!EditorPrefs.HasKey(GameInstallPathKey))
            {
                Debug.LogError("You will need to choose the game install path in \"Find game files\" tab");
                StopAction(builder);
                return;
            }
            
            int currentIndex = builder.buildIndex;

            if (builder.mods == null) return;
            float progress = (currentIndex) / (float)builder.mods.Length;
            if (currentIndex < 0 || currentIndex >= builder.mods.Length) return;
            
            var state = builder.mods[currentIndex];
            GUILayout.Space(12);
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.ProgressBar(rect, progress, $"{builder.actionPrefix} mod: {state.mod?.modSettings.metadata.name}");
        }

        private void StartAction(ChainBuilder builder, string actionPrefix)
        {
            builder.isBuilding = true;
            builder.buildIndex = -1;
            builder.actionPrefix = actionPrefix;
            EditorUtility.SetDirty(builder);
            _coroutine ??= actionPrefix switch
            {
                "Syncing" => EditorCoroutineUtility.StartCoroutine(SyncGameVersions(builder), this),
                "Fetching" => EditorCoroutineUtility.StartCoroutine(FetchModData(builder), this),
                "Building" => EditorCoroutineUtility.StartCoroutine(BuildMods(builder), this),
                _ => throw new ArgumentOutOfRangeException(nameof(actionPrefix), actionPrefix, null)
            };
        }

        private static void StopAction(ChainBuilder builder)
        {
            builder.isBuilding = false;
            builder.buildIndex = -1;
            EditorUtility.SetDirty(builder);
            EditorCoroutineUtility.StopCoroutine(_coroutine);
            _coroutine = null;
        }

        private static IEnumerator SyncGameVersions(ChainBuilder builder)
        {
            while (builder.isBuilding)
            {
                int currentIndex = builder.buildIndex + 1;
                if (builder.mods is null || currentIndex >= builder.mods.Length)
                {
                    StopAction(builder);
                    yield return null;
                }
                var state = builder.mods?[currentIndex];

                bool stop = CheckModValidity(builder, state);
                if (stop)
                {
                    StopAction(builder);
                    yield return null;
                }
                
                builder.buildIndex = currentIndex;
                EditorUtility.SetDirty(builder);

                if (state != null) state.gameVersionTags = builder.allGameVersions;
                Debug.Log($"Synced {state?.mod?.modSettings.metadata.name} Game Version");

                yield return new WaitForSeconds(1f);
            }
        }

        private IEnumerator FetchModData(ChainBuilder builder)
        {
            if (!ModIOExtensions.InitializeModIO())
            {
                StopAction(builder);
                yield return null;
            }
            
            while (builder.isBuilding)
            {
                int currentIndex = builder.buildIndex + 1;
                if (builder.mods is null || currentIndex >= builder.mods.Length)
                {
                    StopAction(builder);
                    yield return null;
                }
                
                var state = builder.mods?[currentIndex];
                
                bool stop = CheckModValidity(builder, state);
                if (stop)
                {
                    StopAction(builder);
                    yield return null;
                }
                
                builder.buildIndex = currentIndex;
                EditorUtility.SetDirty(builder);
                
                var modIoMod = state?.mod;

                long? modID = modIoMod?.modId;
                string metadataName = modIoMod?.modSettings.metadata.name;
                
                if (modIoMod?.modId == 0)
                {
                    Debug.LogError($"ModId not set for mod {metadataName}");
                    StopAction(builder);
                    yield return null;
                }

                var data = new AsyncWrapper<ResultAnd<ModProfile>>(this, GetModAsync(modIoMod));
                yield return data.Coroutine;
                
                var result = data.Result.result;
                if (result.errorCode == 20303 || !result.Succeeded())
                {
                    Debug.LogError($"failed to fetch mod info for mod with ID {modID}: {result.message} (code: {result.errorCode}");
                    StopAction(builder);
                    yield return null;
                }
                
                var value = data.Result.value;
                Debug.Assert(modIoMod != null && modIoMod.modId == value.id);
                
                Debug.Log($"Mod: {metadataName} Last Updated: {value.dateUpdated}");
                
                state.version = value.latestVersion;
                
                state.originalTags = value.tags;
                state.otherTags = string.Join(';', value.tags.Where(tag => !IsVersionTag(tag)));
                state.gameVersionTags = string.Join(';', value.tags.Where(IsVersionTag));
                
                yield return new WaitForSeconds(1f);
            }
        }

        private static bool IsVersionTag(string tag)
        {
            return tag.All(c => c.IsDigit() || c == '.');
        }

        public static IEnumerator GetModAsync(ModSettings modIoMod)
        {
            ResultAnd<ModProfile> result = null;
            ModIOUnity.GetMod(new ModId(modIoMod.modId), resultIn =>
            {
                result = resultIn;
            });
                
            while (result == null)
            {
                yield return new WaitForSeconds(1f);
            }

            yield return result;
        }
        
        private IEnumerator BuildMods(ChainBuilder builder)
        {
            while (builder.isBuilding)
            {
                int currentIndex = builder.buildIndex + 1;
                if (builder.mods is null || currentIndex >= builder.mods.Length)
                {
                    EditorUtility.DisplayDialog("Export", $"Successfully Exported Mods", "OK");
                    StopAction(builder);
                    yield return null;
                }
                var state = builder.mods?[currentIndex];
                
                bool stop = CheckModValidity(builder, state);
                if (stop)
                {
                    StopAction(builder);
                    yield return null;
                }
                
                builder.buildIndex = currentIndex;
                EditorUtility.SetDirty(builder);

                if (state is { shouldBuild: false })
                {
                    if (state.mod != null) Debug.Log($"Skipping {state.mod.modSettings.metadata.name}!");
                }
                else
                {
                    try
                    {
                        BuildLocal(builder, state?.mod);
                        EditorUtility.SetDirty(builder);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        StopAction(builder);
                    }
                }

                yield return new WaitForSeconds(1f);
            }
        }

        private static void BuildLocal(ChainBuilder builder, ModSettings settings)
        {
            string path = EditorPrefs.GetString(GameInstallPathKey);
            
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

            BuildAt(builder, settings, path);
        }

        private static void BuildAt(ChainBuilder builder, ModSettings settings, string path)
        {
            ModBuilder.BuildMod(settings.modSettings, path, success =>
            {
                if (success) return;
                Debug.LogError("Failed to export mod");
                StopAction(builder);
            });
        }
        
        private static bool CheckModValidity(ChainBuilder builder, ModState state)
        {
            if (state is null)
            {
                Debug.LogError($"Chain Builder Mod List not valid!");
                return true;
            }
            if (state.mod is null)
            {
                Debug.LogError($"Mod Settings not set for mod");
                return true;
            }
            if (state.mod.modSettings is null)
            {
                Debug.LogError($"Mod Builder Settings not set for mod");
                StopAction(builder);
                return true;
            }

            return false;
        }
    }
}