using System;
using System.Collections;
using System.IO;
using Markdig;
using PugMod;
using PugMod.ModIO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using ModIO;
using UnityEngine.Serialization;

namespace CoreLib.Editor
{
    [Serializable]
    public class ModState
    {
        [HideInInspector] public string name;

        public ModSettings mod;
        public bool shouldBuild = true;
    }

    [CreateAssetMenu(fileName = "ChainBuilder", menuName = "CoreLib/New ChainBuilder", order = 2)]
    public class ChainBuilder : ScriptableObject
    {
        public ModState[] mods;

        public bool buildAll;

        [Header("Mod IO")] public bool uploadToModIO;
        public bool updateDescription;
        public string uploadVersion;
        [TextArea] public string changeLog;

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

            int currentIndex = builder.buildIndex;

            float progress = (currentIndex) / (float)builder.mods.Length;
            var state = builder.mods[currentIndex];

            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.ProgressBar(rect, progress, $"Building mod \"{state.mod.modSettings.metadata.name}\"");
        }

        private void StartBuilding(ChainBuilder builder)
        {
            builder.isBuilding = true;
            builder.buildIndex = -1;
            EditorUtility.SetDirty(builder);

            _buildCoroutine ??= EditorCoroutineUtility.StartCoroutine(BuildMods(builder), this);
        }

        private static void StopBuilding(ChainBuilder builder)
        {
            builder.isBuilding = false;
            builder.buildIndex = -1;
            EditorUtility.SetDirty(builder);
            EditorCoroutineUtility.StopCoroutine(_buildCoroutine);
            _buildCoroutine = null;
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
                            BuildToModIO(builder, state.mod);
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

            if (BuildAt(builder, settings, tempDirectory))
            {
                return tempDirectory;
            }

            return null;
        }

        private static bool BuildAt(ChainBuilder builder, ModSettings settings, string path)
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
            });
            return buildSuccessful;
        }

        private void BuildToModIO(ChainBuilder builder, ModSettings mod)
        {
            var metadata = mod.modSettings.metadata;

            if (mod.modId == 0)
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

            ModIOUnity.GetMod(new ModId(mod.modId), result =>
            {
                if (!result.result.Succeeded())
                {
                    Debug.LogError($"Couldn't find mod {mod.modSettings.metadata.name} at mod.io");
                    return;
                }

                Debug.Assert(mod.modId == result.value.id);

                var modProfileDetails = new ModProfileDetails
                {
                    modId = new ModId(mod.modId),
                    name = mod.modSettings.metadata.name,
                    logo = mod.logo,
                    summary = mod.summary,
                    visible = result.value.visible
                };

                ModIOUnity.EditModProfile(modProfileDetails, result =>
                {
                    if (!result.Succeeded())
                    {
                        Debug.LogError("Failed to update mod details");
                        StopBuilding(builder);
                        return;
                    }

                    var modPath = BuildTemp(builder, mod);
                    if (string.IsNullOrEmpty(modPath))
                    {
                        return;
                    }

                    Upload(builder, mod, modPath);
                });
            });
        }

        private static bool InitializeModIO()
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

        private void Upload(ChainBuilder builder, ModSettings mod, string path)
        {
            var fileDetails = new ModfileDetails
            {
                modId = new ModId(mod.modId),
                directory = path,
                version = builder.uploadVersion,
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

                Debug.Log($"Successfully uploaded {mod.modSettings.metadata.name}!");
            });

            if (builder.updateDescription)
            {
                UpdateDescription(mod);
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