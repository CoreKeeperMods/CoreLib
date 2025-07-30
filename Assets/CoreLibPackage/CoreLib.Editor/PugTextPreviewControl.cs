using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using I2.Loc;
using UnityEditor;
using UnityEngine;

namespace CoreLib.Editor
{
    [InitializeOnLoad]
    public class PugTextPreviewControl
    {
        private const string MENU_NAME = "Window/Core Keeper Tools/PugText preview";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(MENU_NAME, true);
            set => EditorPrefs.SetBool(MENU_NAME, value);
        }

        static PugTextPreviewControl()
        {
            LoadModTranslations();
            SetPreviewMode(IsEnabled);
        }

        [MenuItem(MENU_NAME)]
        private static void ToggleAction()
        {
            IsEnabled = !IsEnabled;
            SetPreviewMode(IsEnabled);
        }
 
        [MenuItem(MENU_NAME, true)]
        private static bool ToggleActionValidate()
        {
            Menu.SetChecked(MENU_NAME, IsEnabled);
            return true;
        }
        
        private static void SetPreviewMode(bool mode)
        {
            PugTextEditor.InEditorPreviewEnabled = mode;
        }

        const int BufferSize = 128;
        
        [MenuItem("Window/Core Keeper Tools/Load translations", false)]
        public static void LoadModTranslations()
        {
            var sources =  LocalizationManager.Sources;

            string[] guids = AssetDatabase.FindAssets("t:TextAsset");
            var translationFiles = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".csv"))
                .ToArray();

            
            if (translationFiles.Length == 0) return;
            
            LanguageSourceData source;

            if (sources.Count > 0)
            {
                source = sources[0];
                source.ClearAllData();
            }
            else
            {
                source = new LanguageSourceData();
                sources.Add(source);
            }
            
            source.AddLanguage("English", "en");
            
            foreach (string translationFile in translationFiles)
            {
                using var fileStream = File.OpenRead(translationFile);
                using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize);
                
                string line;
                int lineIndex = 0;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (lineIndex == 0)
                    {
                        if (!line.StartsWith("Key\tType\tDesc\tEnglish")) break;
                    }

                    string[] parts = line.Split('\t');
                    if (parts.Length < 4) continue;

                    string key = parts[0];
                    string value = parts[3];
                    
                    AddTerm(source, key, value);

                    lineIndex++;
                }
            }


        }
        
        internal static void AddTerm(LanguageSourceData source, string term, string value)
        {
            if (source.mDictionary.ContainsKey(term))
            {
                return;
            }
            
            TermData termdata = new TermData
            {
                Term = term,
                TermType = eTermType.Text,
                Flags = new byte[source.mLanguages.Count]
            };

            List<string> languages = new List<string>
            {
                value
            };

            termdata.Languages = languages.ToArray();
            source.mDictionary.Add(termdata.Term, termdata);
            source.mTerms.Add(termdata);
        }
    }
}