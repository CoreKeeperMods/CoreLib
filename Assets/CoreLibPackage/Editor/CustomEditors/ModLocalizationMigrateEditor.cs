using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CoreLib.Editor
{
    /// <summary>
    /// This editor helps to migrate localizations in localization.csv (tsv) format to Core Keeper's Data Block format
    /// </summary>
    [CustomEditor(typeof(TextAsset))]
    public class ModLocalizationMigrateEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor _baseEditor;

        private void OnEnable()
        {
            // Find the internal TextAssetInspector type
            Type type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.TextAssetInspector");
            if (type != null)
            {
                _baseEditor = CreateEditor(targets, type);
            }
        }

        private void OnDisable()
        {
            if (_baseEditor != null)
                DestroyImmediate(_baseEditor);
        }

        public override void OnInspectorGUI()
        {
            // Draw the default TextAsset inspector (text area/edit box)
            if (_baseEditor != null)
                _baseEditor.OnInspectorGUI();

            // 1. Check if the filename matches 'localization'
            string assetPath = AssetDatabase.GetAssetPath(target);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            // StringComparison makes it case-insensitive if desired
            if (fileName.Equals("localization", StringComparison.OrdinalIgnoreCase))
            {
                DrawCustomUI();
            }
        }

        private void DrawCustomUI()
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = true;

            // Add your custom buttons or UI here
            if (GUILayout.Button("Migrate localization to Data Blocks!"))
            {
                DoMigration();
            }

            GUI.enabled = wasEnabled;
        }

        private readonly HashSet<string> _warnedLanguages = new HashSet<string>();
        private readonly Dictionary<string, TextDataBlock> _addedBlocks = new Dictionary<string, TextDataBlock>();

        private void DoMigration()
        {
            Debug.Log("Starting localization migration for " + target.name);
            _warnedLanguages.Clear();
            _addedBlocks.Clear();

            if (!ValidateDirectory(out string dataBlockDir)) return;
            if (!ReadFile(out string[] lines, out string[] header)) return;

            var languages = ScriptableData.GetDataBlocks<LanguageDataBlock>();

            // Pass 1: Create all instances first
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Split('\t');
                if (line.Length < 4) continue;

                string fullKey = line[0];
                if (fullKey.Length == 0)
                {
                    Debug.Log("Skipping empty line!");
                    continue;
                }
                
                bool isDesc = fullKey.EndsWith("Desc");
                string baseKey = isDesc ? fullKey[..^4] : fullKey;

                if (_addedBlocks.ContainsKey(baseKey)) continue;

                var textBlock = CreateInstance<TextDataBlock>();

                foreach (var language in languages)
                    textBlock.ValidateForLanguage(language);

                string key = baseKey.Replace(':', '_');
                string fileDirectory = dataBlockDir;

                if (key.Contains('/'))
                {
                    var keyParts = key.Split('/');
                    fileDirectory = Path.Combine(keyParts.SkipLast(1).Prepend(dataBlockDir).ToArray());
                    key = keyParts.Last();
                }

                SerializedObject so = new SerializedObject(textBlock);

                so.FindProperty("m_Name").stringValue = key;
                if (!isDesc) so.FindProperty("m_localizationHint").stringValue = line[2];

                so.ApplyModifiedProperties();

                SaveDataBlock(fileDirectory, textBlock, baseKey);
            }

            // Pass 2: Apply localization data
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Split('\t');
                if (line.Length < 4) continue;

                string fullKey = line[0];
                if (fullKey.Length == 0)
                {
                    Debug.Log("Skipping empty line!");
                    continue;
                }
                
                bool isDesc = fullKey.EndsWith("Desc");
                string baseKey = isDesc ? fullKey[..^4] : fullKey;

                if (!_addedBlocks.TryGetValue(baseKey, out var textBlock)) continue;

                for (int j = 3; j < header.Length; j++)
                {
                    var languageName = header[j]
                        .Replace("(France)", "") // Remove country specifiers because new language assets don't have those
                        .Replace("(Italy)", "")
                        .Trim();
                    
                    var value = line[j].Trim();
                    var languageEntry = languages.FirstOrDefault(l => l.name.Contains(languageName));

                    if (languageEntry == null)
                    {
                        WarnLanguageMissing(languageName);
                        continue;
                    }

                    var currentEntry = textBlock.GetLocalized(languageEntry);
                    if (isDesc)
                        textBlock.EditorSetLocalization(languageEntry, currentEntry.title, value);
                    else
                        textBlock.EditorSetLocalization(languageEntry, value, currentEntry.description);
                }

                // Mark as dirty after modification
                AssetDatabase.SaveAssetIfDirty(textBlock);
            }

            LanguageDataBlock primaryLanguage = LanguageDataBlock.GetPrimaryLanguage();

            foreach (var block in _addedBlocks.Values)
            {
                var primary = block.GetLocalized(primaryLanguage);
                block.MarkAsLocalized(primary);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Localization migration successfully finished!");
        }

        private void SaveDataBlock(string fileDirectory, TextDataBlock textBlock, string baseKey)
        {
            var absDirectory = Path.GetFullPath(fileDirectory);

            if (!Directory.Exists(absDirectory))
                Directory.CreateDirectory(absDirectory);

            var finalPath = Path.Combine(fileDirectory, $"{textBlock.name}.asset");
            Debug.Log($"Creating DataBlock at {finalPath}");

            AssetDatabase.CreateAsset(textBlock, finalPath);
            _addedBlocks.TryAdd(baseKey, textBlock);
        }

        private void WarnLanguageMissing(string languageName)
        {
            if (_warnedLanguages.Contains(languageName)) return;

            Debug.LogWarning($"Failed to find language {languageName}!");
            _warnedLanguages.Add(languageName);
        }

        private bool ReadFile(out string[] lines, out string[] header)
        {
            TextAsset textAsset = (TextAsset)target;
            string content = textAsset.text;

            lines = content.Split('\n');
            header = null;

            if (lines.Length < 2)
            {
                Debug.LogError("Localization migration failed! Localization file is empty or corrupted!");
                return false;
            }

            header = lines[0].Split('\t');

            if (header.Length < 4 ||
                header[0] != "Key" ||
                header[1] != "Type" ||
                header[2] != "Desc")
            {
                Debug.LogError("Localization migration failed! Localization file seems to be corrupted!");
                return false;
            }

            return true;
        }

        private bool ValidateDirectory(out string dataBlockDir)
        {
            dataBlockDir = null;

            string assetPath = AssetDatabase.GetAssetPath(target);
            string directory = Path.GetDirectoryName(assetPath);
            if (directory == null)
            {
                Debug.LogWarning("Failed to determine file directory!");
                return false;
            }

            string dataDir = Path.Combine(directory, "..", "Data");
            dataDir = Path.GetFullPath(dataDir).Replace('\\', '/');

            if (!Directory.Exists(dataDir))
            {
                Debug.LogError("Localization migration failed! DataBlock directory not found! Please create mod DataBlock directory! If setup correctly you should have both Data and Localization folders in the mod folder.");
                return false;
            }

            dataBlockDir = "Assets" + dataDir[Application.dataPath.Length..];
            dataBlockDir = Path.Combine(dataBlockDir, "TextDataBlock");
            return true;
        }

        // Keep the original bottom preview window
        public override bool HasPreviewGUI() => _baseEditor != null && _baseEditor.HasPreviewGUI();
        public override void OnPreviewGUI(Rect r, GUIStyle background) => _baseEditor.OnPreviewGUI(r, background);
    }
}