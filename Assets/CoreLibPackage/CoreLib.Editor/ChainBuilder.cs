using System;
using System.Collections;
using System.IO;
using System.Linq;
using Markdig;
using Markdig.Helpers;
using PugMod;
using PugMod.ModIO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using ModIO;

namespace CoreLib.Editor
{
    [Serializable]
    public class ModState
    {
        [HideInInspector] public string name;

        public ModSettings mod;
        public bool shouldBuild = true;
        public string version;

        [HideInInspector]
        public string[] originalTags = Array.Empty<string>();
        
        [ModIOTag(inverted = true)]
        public string otherTags;
        
        [ModIOTag]
        public string gameVersionTags;
    }

    [CreateAssetMenu(fileName = "ChainBuilder", menuName = "CoreLib/New ChainBuilder", order = 2)]
    public class ChainBuilder : ScriptableObject
    {
        public ModState[] mods;

        public bool buildAll;

        public bool uploadToModIO;
        public bool updateDescription;
        public string uploadVersion;
        
        [ModIOTag]
        public string allGameVersions;
        
        [TextArea] public string changeLog;

        [HideInInspector] public string actionPrefix = "Building";
        [HideInInspector] public int buildIndex;
        [HideInInspector] public bool isBuilding;
        [HideInInspector] public bool oldBuildAll;

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(uploadVersion))
            {
                var parts = uploadVersion.Split('.');
                if (parts.Length != 3)
                {
                    Array.Resize(ref parts, 3);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        parts[i] ??= "0";
                    }

                    uploadVersion = string.Join('.', parts);
                }
            }

            if (buildAll != oldBuildAll)
            {
                foreach (ModState state in mods)
                {
                    state.shouldBuild = buildAll;
                }

                oldBuildAll = buildAll;
            }

            if (mods.Length > 0)
            {
                bool all = true;
                foreach (ModState state in mods)
                {
                    state.name = $"{state.mod.modSettings.metadata.name}{(state.shouldBuild ? "" : " (Disabled)")}";
                    
                    all &= state.shouldBuild;
                }

                if (all != buildAll)
                {
                    buildAll = all;
                    oldBuildAll = all;
                }
            }
        }
    }

    [CustomEditor(typeof(ChainBuilder))]
    public class ChainBuilderEditor : UnityEditor.Editor
    {
        private const string GAME_INSTALL_PATH_KEY = "PugMod/SDKWindow/GamePath";

        private static EditorCoroutine _buildCoroutine;
        private static EditorCoroutine _fetchCoroutine;
        private static EditorCoroutine _updateCoroutine;
        
        private static MarkdownPipeline _pipeline;
        
        public static MarkdownPipeline pipeline
        {
            get
            {
                _pipeline ??= new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .UseHeadingUpper()
                    .Build();

                return _pipeline;
            }
        }

        public override bool RequiresConstantRepaint()
        {
            ChainBuilder builder = (ChainBuilder)target;
            return builder.isBuilding;
        }

        public override void OnInspectorGUI()
        {
            ChainBuilder builder = (ChainBuilder)target;
            
            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty iterator = serializedObject.GetIterator();
            for (
                bool enterChildren = true;
                iterator.NextVisible(enterChildren);
                enterChildren = false
            )
            {
                if (iterator.propertyPath == "uploadToModIO")
                {
                    GUILayout.Space(12);
                    EditorGUILayout.LabelField("Mod IO", EditorStyles.boldLabel);

                    using (new EditorGUI.DisabledScope(builder.isBuilding))
                    {
                        if (GUILayout.Button("Fetch Data"))
                            FetchDataFromModIO(builder);

                        if (GUILayout.Button("Update Mod IO Data"))
                            StartUpdating(builder);
                    }
                    
                    GUILayout.Space(8);
                    EditorGUILayout.PropertyField(iterator, true);
                } else if (iterator.propertyPath == "allGameVersions")
                {
                    GUILayout.Space(8);
                    EditorGUILayout.PropertyField(iterator, true);

                    if (GUILayout.Button("Sync Game Versions"))
                    {
                        foreach (var mod in builder.mods)
                        {
                            mod.gameVersionTags = builder.allGameVersions;
                        }
                    }
                    GUILayout.Space(12);
                }
                else
                {
                    using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                        EditorGUILayout.PropertyField(iterator, true);
                }
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();

            using (new EditorGUI.DisabledScope(builder.isBuilding))
            {
                if (GUILayout.Button("Build All"))
                    StartBuilding(builder);
            }

            if (!builder.isBuilding) return;
            
            if (!EditorPrefs.HasKey(GAME_INSTALL_PATH_KEY))
            {
                Debug.LogError("You will need to choose the game install path in \"Find game files\" tab");
                StopBuilding(builder);
                return;
            }

            int currentIndex = builder.buildIndex;

            float progress = (currentIndex) / (float)builder.mods.Length;
            if (currentIndex < 0 || currentIndex >= builder.mods.Length) return;
            
            var state = builder.mods[currentIndex];

            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.ProgressBar(rect, progress, $"{builder.actionPrefix} mod \"{state.mod.modSettings.metadata.name}\"");
        }

        private void FetchDataFromModIO(ChainBuilder builder)
        {
            builder.isBuilding = true;
            builder.buildIndex = -1;
            builder.actionPrefix = "Fetching";
            EditorUtility.SetDirty(builder);
            
            _fetchCoroutine ??= EditorCoroutineUtility.StartCoroutine(FetchModData(builder), this);
        }

        private void StartBuilding(ChainBuilder builder)
        {
            builder.isBuilding = true;
            builder.buildIndex = -1;
            builder.actionPrefix = "Building";
            EditorUtility.SetDirty(builder);

            _buildCoroutine ??= EditorCoroutineUtility.StartCoroutine(BuildMods(builder), this);
        }

        private void StartUpdating(ChainBuilder builder)
        {
            builder.isBuilding = true;
            builder.buildIndex = -1;
            builder.actionPrefix = "Updating";
            EditorUtility.SetDirty(builder);

            _updateCoroutine ??= EditorCoroutineUtility.StartCoroutine(UpdateMods(builder), this);
        }

        private static void StopBuilding(ChainBuilder builder) => StopAction(builder, ref _buildCoroutine);
        private static void StopFetch(ChainBuilder builder) => StopAction(builder, ref _fetchCoroutine);
        
        public static void StopUpdate(ChainBuilder builder) => StopAction(builder, ref _updateCoroutine);

        private static void StopAction(ChainBuilder builder, ref EditorCoroutine action)
        {
            builder.isBuilding = false;
            builder.buildIndex = -1;
            EditorUtility.SetDirty(builder);
            EditorCoroutineUtility.StopCoroutine(action);
            action = null;
        }

        private IEnumerator FetchModData(ChainBuilder builder)
        {
            if (!InitializeModIO())
            {
                StopFetch(builder);
                yield return null;
            }
            
            while (builder.isBuilding)
            {
                int currentIndex = builder.buildIndex + 1;
                if (currentIndex >= builder.mods.Length)
                {
                    StopFetch(builder);
                    yield return null;
                }

                var state = builder.mods[currentIndex];

                builder.buildIndex = currentIndex;
                EditorUtility.SetDirty(builder);

                var modIoMod = state.mod;
                var metadata = modIoMod.modSettings.metadata;

                if (modIoMod.modId == 0)
                {
                    Debug.LogError($"ModId not set for mod {metadata.name}");
                    StopFetch(builder);
                    yield return null;
                }

                var data = new AsyncWrapper<ResultAnd<ModProfile>>(this, GetModAsync(modIoMod));
                yield return data.coroutine;

                var result = data.result.result;
                if (result.errorCode == 20303 || !result.Succeeded())
                {
                    Debug.LogError($"failed to fetch mod info for mod with ID {modIoMod.modId}: {result.message} (code: {result.errorCode}");
                    StopFetch(builder);
                    yield return null;
                }

                var value = data.result.value;
                Debug.Assert(modIoMod.modId == value.id);
                
                Debug.Log($"Date updated: {value.dateUpdated}");

                state.version = value.latestVersion;

                state.originalTags = value.tags;
                state.otherTags = string.Join(';', value.tags.Where(tag => !IsVersionTag(tag)));
                state.gameVersionTags = string.Join(';', value.tags.Where(IsVersionTag));

                yield return new WaitForSeconds(0.2f);
            }
        }

        private IEnumerator UpdateMods(ChainBuilder builder)
        {
            if (!InitializeModIO())
            {
                StopUpdate(builder);
                yield return null;
            }
            
            while (builder.isBuilding)
            {
                int currentIndex = builder.buildIndex + 1;
                if (currentIndex >= builder.mods.Length)
                {
                    StopUpdate(builder);
                    yield return null;
                }

                var state = builder.mods[currentIndex];

                builder.buildIndex = currentIndex;
                EditorUtility.SetDirty(builder);

                if (!state.shouldBuild)
                {
                    Debug.Log($"Skipping {state.mod.modSettings.metadata.name}!");
                    continue;
                }
                
                var modIoMod = state.mod;
                var metadata = modIoMod.modSettings.metadata;

                if (modIoMod.modId == 0)
                {
                    Debug.LogError($"ModId not set for mod {metadata.name}");
                    StopUpdate(builder);
                    yield return null;
                }

                var newTags = state.gameVersionTags.Split(';').Union(state.otherTags.Split(';')).Where(s => s != "").ToArray();
                
                var deleteTags = state.originalTags.Where(tag => !newTags.Contains(tag)).ToArray();
                var addTags = newTags.Where(tag => !state.originalTags.Contains(tag)).ToArray();

                var modId = new ModId(modIoMod.modId);

                if (deleteTags.Length > 0)
                {
                    bool done = false;
                    ModIOUnity.DeleteTags(modId, deleteTags, result =>
                    {
                        if (!result.Succeeded())
                        {
                            Debug.LogError($"Failed to update mod details: {result.message} (code: {result.errorCode}");
                            StopUpdate(builder);
                        }
                        done = true;
                    });
                    
                    while (!done)
                    {
                        yield return new WaitForSeconds(0.2f);
                    }
                }

                yield return new WaitForSeconds(0.5f);

                if (addTags.Length > 0)
                {
                    bool done = false;
                    ModIOUnity.AddTags(modId, addTags, result =>
                    {
                        if (!result.Succeeded())
                        {
                            Debug.LogError($"Failed to update mod details: {result.message} (code: {result.errorCode}");
                            StopUpdate(builder);
                        }
                        done = true;
                    });
                    
                    while (!done)
                    {
                        yield return new WaitForSeconds(0.2f);
                    }
                }

                Debug.Log($"Mod {metadata.name} mod io data has been updated!");
                yield return new WaitForSeconds(0.5f);
            }
        }

        internal bool IsVersionTag(string tag)
        {
            return tag.All(c => c.IsDigit() || c == '.');
        }

        private static IEnumerator GetModAsync(ModSettings modIoMod)
        {
            ResultAnd<ModProfile> result = null;
            ModIOUnity.GetMod(new ModId(modIoMod.modId), resultIn =>
            {
                result = resultIn;
            });
                
            while (result == null)
            {
                yield return new WaitForSeconds(0.2f);
            }

            yield return result;
        }

        private IEnumerator BuildMods(ChainBuilder builder)
        {
            while (builder.isBuilding)
            {
                int currentIndex = builder.buildIndex + 1;
                if (currentIndex >= builder.mods.Length)
                {
                    StopBuilding(builder);
                    yield return null;
                }

                var state = builder.mods[currentIndex];

                builder.buildIndex = currentIndex;
                EditorUtility.SetDirty(builder);

                if (!state.shouldBuild)
                {
                    Debug.Log($"Skipping {state.mod.modSettings.metadata.name}!");
                }
                else
                {
                    try
                    {
                        if (builder.uploadToModIO)
                            BuildToModIO(builder, state);
                        else
                            BuildLocal(builder, state.mod);

                        EditorUtility.SetDirty(builder);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        StopBuilding(builder);
                    }
                }

                yield return new WaitForSeconds(1);
            }
        }

        private static void BuildLocal(ChainBuilder builder, ModSettings settings)
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

            BuildAt(builder, settings, path);
        }

        private static string BuildTemp(ChainBuilder builder, ModSettings settings)
        {
            var tempDirectory = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString());

            Directory.CreateDirectory(tempDirectory);

            if (BuildAt(builder, settings, tempDirectory, false))
            {
                return tempDirectory;
            }

            return null;
        }

        private static bool BuildAt(ChainBuilder builder, ModSettings settings, string path, bool installInSubDirectory = true)
        {
            bool buildSuccessful = false;
            ModBuilder.BuildMod(settings.modSettings, path, success =>
            {
                if (!success)
                {
                    Debug.LogError("Failed to export mod");
                    StopBuilding(builder);
                    return;
                }

                Debug.Log($"Mod {settings.modSettings.metadata.name} successfully exported to {path}");
                buildSuccessful = true;
            }, installInSubDirectory);
            return buildSuccessful;
        }

        private void BuildToModIO(ChainBuilder builder, ModState mod)
        {
            var metadata = mod.mod.modSettings.metadata;

            if (mod.mod.modId == 0)
            {
                Debug.LogError($"ModId not set for mod {metadata.name}");
                StopBuilding(builder);
                return;
            }

            if (!InitializeModIO())
            {
                StopBuilding(builder);
                return;
            }

            ModIOUnity.GetMod(new ModId(mod.mod.modId), result =>
            {
                if (result.result.errorCode == 20303)
                {
                    Debug.LogError($"couldn't find mod with id {mod.mod.modId}");
                    return;
                }
					
                if (!result.result.Succeeded())
                {
                    Debug.LogError($"failed to fetch mod info for mod with ID {mod.mod.modId}: {result.result.message} (code: {result.result.errorCode}");
                    return;
                }

                Debug.Assert(mod.mod.modId == result.value.id);

                var modProfileDetails = new ModProfileDetails
                {
                    modId = new ModId(mod.mod.modId),
                    name = mod.mod.modSettings.metadata.name,
                    logo = mod.mod.logo,
                    summary = mod.mod.summary,
                    visible = result.value.visible
                };

                ModIOUnity.EditModProfile(modProfileDetails, result =>
                {
                    if (!result.Succeeded())
                    {
                        Debug.LogError($"Failed to update mod details: {result.message} (code: {result.errorCode}");
                        StopBuilding(builder);
                        return;
                    }

                    var modPath = BuildTemp(builder, mod.mod);
                    if (string.IsNullOrEmpty(modPath))
                    {
                        return;
                    }

                    Upload(builder, mod, modPath);
                });
            });
        }

        internal static bool InitializeModIO()
        {
            if (!ModIOUnity.IsInitialized())
            {
                Result result = ModIOUnity.InitializeForUser("PugModSDKUser");

                if (!result.Succeeded())
                {
                    Debug.Log("Failed to initialize mod.io SDK");
                    return false;
                }
            }

            return true;
        }

        private void Upload(ChainBuilder builder, ModState mod, string path)
        {
            var version = string.IsNullOrWhiteSpace(mod.version) ? builder.uploadVersion : mod.version;
            
            var fileDetails = new ModfileDetails
            {
                modId = new ModId(mod.mod.modId),
                directory = path,
                version = version,
                changelog = builder.changeLog
            };

            ModIOUnity.UploadModfile(fileDetails, result =>
            {
                if (!result.Succeeded())
                {
                    Debug.LogError($"Failed to upload mod to mod.io {result.message}");
                    StopBuilding(builder);
                    return;
                }

                Debug.Log($"Successfully uploaded {mod.mod.modSettings.metadata.name}!");
            });

            if (builder.updateDescription)
            {
                UpdateDescription(mod.mod);
            }
        }

        private static void UpdateDescription(ModSettings mod)
        {
            var assets = Application.dataPath;
            var modPath = mod.modSettings.modPath;

            var readme = Path.Combine(assets, "..", modPath, "README.md");
            if (File.Exists(readme))
            {
                var readmeText = File.ReadAllText(readme);
                var readmeHtml = Markdown.ToHtml(readmeText, pipeline);
                var profileEdit = new ModProfileDetails()
                {
                    modId = new ModId(mod.modId),
                    description = readmeHtml
                };

                ModIOUnity.EditModProfile(profileEdit, result =>
                {
                    Debug.Log($"Updated description for {mod.modSettings.metadata.name}!");
                });
            }
        }
    }
}