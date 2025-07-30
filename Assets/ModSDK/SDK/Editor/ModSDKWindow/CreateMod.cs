using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using EditorUtility = UnityEditor.EditorUtility;

// ReSharper disable once CheckNamespace
namespace PugMod
{
    public partial class ModSDKWindow
    {
        private class CreateMod
        {
            private DropdownField _modList;
            private TextField _modNameField;
            private Button _createButton;
            private Label _gameInstallPath;
            private Button _localExportButton;
            private ListView _modDependencyList;
            private Toggle _requiredMultiplayerToggle;

            private VisualElement _createForm;
            private VisualElement _localExportForm;

            private string _hintName;
            private readonly List<ModDependencyEntry> _dependencies = new();
            private List<ModBuilderSettings> _modSettings;
            private readonly Dictionary<VisualElement, object> _callbacks = new();

            public void Refresh()
            {
                if (EditorPrefs.HasKey(CHOSEN_MOD_KEY))
                {
                    _modList.index = _modList.choices.IndexOf(EditorPrefs.GetString(CHOSEN_MOD_KEY));
                    UpdateSelection();
                }

                _gameInstallPath.text = EditorPrefs.HasKey(GAME_INSTALL_PATH_KEY)
                    ? $"Install path: {EditorPrefs.GetString(GAME_INSTALL_PATH_KEY)}"
                    : "Go to 'Find game files' to set correct path for mod loading.";
            }

            public void OnEnable(VisualElement root)
            {
                _modList = root.Q<DropdownField>("CreateModModList");
                _modNameField = root.Q<TextField>("CreateModName");
                _createButton = root.Q<Button>("CreateModButton");
                _gameInstallPath = root.Q<Label>("LocalExportGamePath");
                _localExportButton = root.Q<Button>("LocalExportButton");
                _modDependencyList = root.Q<ListView>("ModDependencies");
                _requiredMultiplayerToggle = root.Q<Toggle>("RequiredOnServerAndClient");

                _createForm = root.Q<VisualElement>("CreateModForm");
                _localExportForm = root.Q<VisualElement>("LocalExportForm");

                var modDependencyEntryTemplate =
                    AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/ModSDK/UI/ModDependencyEntry.uxml");

                _modSettings = new List<ModBuilderSettings>(AssetDatabase.FindAssets("t:PugMod.ModBuilderSettings")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<ModBuilderSettings>));

                _modList.choices.AddRange(_modSettings.Select(x => x.metadata.name));

                _modList.RegisterCallback<ChangeEvent<string>>(_ => { UpdateSelection(); });

                Refresh();

                if (_modList.index == -1)
                {
                    _modList.index = 0;
                }

                _hintName = _modNameField.value;

                _createButton.clicked += () =>
                {
                    if (string.IsNullOrEmpty(_modNameField.value) || string.Equals(_modNameField.value, _hintName))
                    {
                        ShowError("No mod name specified");
                        return;
                    }

                    ModBuilderWindow.CreateNewMod(_modNameField.value);

                    EditorPrefs.SetString(CHOSEN_MOD_KEY, _modNameField.value);
                };

                _modDependencyList.fixedItemHeight = 40;
                _modDependencyList.makeItem = () => modDependencyEntryTemplate.CloneTree();
                _modDependencyList.bindItem = (element, index) =>
                {
                    _dependencies[index] ??= new ModDependencyEntry();

                    var toggle = element.Q<Toggle>("ModDependencyEntryRequired");
                    toggle.value = _dependencies[index].Value.required;

                    if (_callbacks.Remove(toggle, out var existingCallback))
                    {
                        toggle.UnregisterValueChangedCallback((EventCallback<ChangeEvent<bool>>)existingCallback);
                    }

                    var newCallback = new EventCallback<ChangeEvent<bool>>(evt => OnToggleValueChanged(evt, index));
                    toggle.RegisterValueChangedCallback(newCallback);
                    _callbacks[toggle] = newCallback;

                    var depNameElement = element.Q<TextField>("ModDependencyEntryName");
                    depNameElement.value = _dependencies[index].Value.modName;

                    if (_callbacks.Remove(depNameElement, out var existingTextCallback))
                    {
                        depNameElement.UnregisterValueChangedCallback(
                            (EventCallback<ChangeEvent<string>>)existingTextCallback);
                    }

                    var newTextCallback =
                        new EventCallback<ChangeEvent<string>>(evt => OnTextFieldValueChanged(evt, index));
                    depNameElement.RegisterValueChangedCallback(newTextCallback);
                    _callbacks[depNameElement] = newTextCallback;

                    SyncDependencyListToScriptableObject();
                };
                _modDependencyList.unbindItem = (element, _) =>
                {
                    var toggle = element.Q<Toggle>("ModDependencyEntryRequired");

                    if (_callbacks.Remove(toggle, out var existingCallback))
                    {
                        toggle.UnregisterValueChangedCallback((EventCallback<ChangeEvent<bool>>)existingCallback);
                    }

                    var depNameElement = element.Q<TextField>("ModDependencyEntryName");
                    if (_callbacks.Remove(depNameElement, out var existingTextCallback))
                    {
                        depNameElement.UnregisterValueChangedCallback(
                            (EventCallback<ChangeEvent<string>>)existingTextCallback);
                    }

                    SyncDependencyListToScriptableObject();
                };
                _modDependencyList.itemsSource = _dependencies;

                _gameInstallPath.text = EditorPrefs.HasKey(GAME_INSTALL_PATH_KEY)
                    ? $"Install path: {EditorPrefs.GetString(GAME_INSTALL_PATH_KEY)}"
                    : "Go to 'Find game files' to set correct path for mod loading.";

                _localExportButton.clicked += () =>
                {
                    if (_modList.choices == null || _modList.index < 0 || _modList.index >= _modList.choices.Count)
                    {
                        ShowError("No mod chosen");
                        return;
                    }

                    var modSettings = _modSettings.Find(x => x.metadata.name.Equals(_modList.choices[_modList.index]));

                    if (modSettings is null)
                    {
                        ShowError("Can't find mod settings");
                        return;
                    }

                    if (!EditorPrefs.HasKey(GAME_INSTALL_PATH_KEY))
                    {
                        ShowError("You will need to choose the game install path in \"Find game files\" tab");
                        return;
                    }

                    var path = EditorPrefs.GetString(GAME_INSTALL_PATH_KEY);

                    if (Directory.Exists(Path.Combine(path, "CoreKeeper_Data")))
                    {
                        path = Path.Combine(path, "CoreKeeper_Data", "StreamingAssets", "Mods");
                    }
                    else if (Directory.Exists(Path.Combine(path, "CoreKeeperServer_Data")))
                    {
                        path = Path.Combine(path, "CoreKeeperServer_Data", "StreamingAssets", "Mods");
                    }
                    else if (Directory.Exists(Path.Combine(path, "Assets")))
                    {
                        // Installing to another Unity project
                        path = Path.Combine(path, "Assets", "StreamingAssets", "Mods");
                    }
                    else
                    {
                        ShowError($"Can't find game at {path}");
                        return;
                    }

                    ModBuilder.BuildMod(modSettings, path, success =>
                    {
                        if (!success)
                        {
                            ShowError("Failed to export mod");
                            return;
                        }

                        EditorUtility.DisplayDialog("Export",
                            $"Mod {modSettings.metadata.name} successfully exported to {path}", "OK");
                    });
                };

                _requiredMultiplayerToggle.RegisterValueChangedCallback(evt =>
                {
                    if (!GetModSettings(out var modBuilderSettings)) return;
                    modBuilderSettings.metadata.requiredOn = evt.newValue
                        ? ModMetadata.ModExistsOn.ClientAndServer
                        : ModMetadata.ModExistsOn.None;
                    EditorUtility.SetDirty(modBuilderSettings);
                    AssetDatabase.SaveAssetIfDirty(modBuilderSettings);
                });

                UpdateSelection();
            }

            private void OnToggleValueChanged(ChangeEvent<bool> evt, int index)
            {
                var tmp = _dependencies[index];
                tmp.Value.required = evt.newValue;
                _dependencies[index] = tmp;

                if (!GetModSettings(out var modBuilderSettings)) return;
                var metadataDependency = modBuilderSettings.metadata.dependencies[index];
                metadataDependency.required = evt.newValue;
                modBuilderSettings.metadata.dependencies[index] = metadataDependency;
                EditorUtility.SetDirty(modBuilderSettings);
                AssetDatabase.SaveAssetIfDirty(modBuilderSettings);
            }

            private void OnTextFieldValueChanged(ChangeEvent<string> evt, int index)
            {
                var tmp = _dependencies[index];
                tmp.Value.modName = evt.newValue;
                _dependencies[index] = tmp;

                if (!GetModSettings(out var modBuilderSettings)) return;
                var metadataDependency = modBuilderSettings.metadata.dependencies[index];
                metadataDependency.modName = evt.newValue;
                modBuilderSettings.metadata.dependencies[index] = metadataDependency;
                EditorUtility.SetDirty(modBuilderSettings);
                //AssetDatabase.SaveAssetIfDirty(modBuilderSettings);
            }

            private void SyncDependencyListToScriptableObject()
            {
                if (!GetModSettings(out var modBuilderSettings))
                {
                    return;
                }

                modBuilderSettings.metadata.dependencies.Clear();

                foreach (var dep in _dependencies)
                {
                    if (dep == null)
                    {
                        continue;
                    }

                    modBuilderSettings.metadata.dependencies.Add(dep.Value);
                }

                EditorUtility.SetDirty(modBuilderSettings);
                AssetDatabase.SaveAssetIfDirty(modBuilderSettings);
            }

            private void UpdateSelection()
            {
                if (!GetModSettings(out var modBuilderSettings))
                {
                    _createForm.style.display = DisplayStyle.Flex;
                    _localExportForm.style.display = DisplayStyle.None;
                    EditorPrefs.DeleteKey(CHOSEN_MOD_KEY);
                    return;
                }

                EditorPrefs.SetString(CHOSEN_MOD_KEY, modBuilderSettings.metadata.name);

                _createForm.style.display = DisplayStyle.None;
                _localExportForm.style.display = DisplayStyle.Flex;

                _requiredMultiplayerToggle.value =
                    modBuilderSettings.metadata.requiredOn == ModMetadata.ModExistsOn.ClientAndServer;

                _dependencies.Clear();
                foreach (var dep in modBuilderSettings.metadata.dependencies)
                {
                    _dependencies.Add(new ModDependencyEntry
                    {
                        Value = dep,
                    });
                }

                _modDependencyList.RefreshItems();
            }

            private bool GetModSettings(out ModBuilderSettings modBuilderSettings, string modName = null)
            {
                if (modName == null)
                {
                    if (_modList.choices == null || _modList.index < 0 || _modList.index >= _modList.choices.Count)
                    {
                        modBuilderSettings = null;
                        return false;
                    }

                    modName = _modList.choices[_modList.index];
                }

                modBuilderSettings = _modSettings.Find(x => x.metadata.name.Equals(modName));

                return modBuilderSettings != null;
            }

            private class ModDependencyEntry
            {
                public ModMetadata.Dependency Value;
            }
        }
    }
}