using System;
using System.Collections.Generic;
using System.Linq;
using I2.Loc;
using PugMod;
using Logger = CoreLib.Util.Logger;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Localization
{
    //TODO Remove Localization Module?
    /// Provides functionality for managing localization strings and entity localization within the game.
    public class LocalizationModule : BaseSubmodule
    {
        #region Fields
        
        internal static LanguageSourceData localizationSourceData;

        #endregion
        
        #region BaseSubmodule Implementation
        
        public const string NAME = "Core Library - Localization";
        
        internal static Logger log = new(NAME);
        
        internal static LocalizationModule Instance => CoreLibMod.GetModuleInstance<LocalizationModule>();
        
        internal override void Load()
        {
            base.Load();
            localizationSourceData = new LanguageSourceData();
            DependentMods.ForEach(ExtractTermsFromModdedLocalizationTables);
        }

        internal override void LateLoad()
        {
            // Doping this late to ensure the source is last and doesn't interfere with game code
            localizationSourceData.Awake();
        }

        #endregion
        
        #region Public Interface

        /// Add a new localization term with specified translations.
        /// <param name="term">Unique identifier for the localization term.</param>
        /// <param name="translations">Dictionary containing translations for each language.</param>
        /// <exception cref="ArgumentException">Thrown if the translation dictionary does not include an English ("en") translation or if the term is already registered.</exception>
        public static void AddTerm(string term, Dictionary<string, string> translations)
        {
            if (!translations.ContainsKey("en"))
                throw new ArgumentException("Translation dictionary must contain english translation!");
            foreach ((string key, string value) in translations)
            {
                if(string.IsNullOrEmpty(value)) continue;
                var languageTerm = new LanguageTerm
                {
                    localizationCodeDropdown = GoogleLanguages.GetLanguagesForDropdown("","").First(str => str.Split('[').Last().Contains(key)),
                    translation = value
                };
                AddTerm_Internal(term, languageTerm);
            }
            if(Instance.Loaded)
                LocalizationManager.UpdateSources();
        }

        /// Adds a new localization term with English and optional Chinese translations.
        /// <param name="term">A unique identifier for the localization term.</param>
        /// <param name="en">The English translation of the term.</param>
        /// <param name="cn">The optional Chinese translation of the term. Defaults to an empty string.</param>
        /// <exception cref="InvalidOperationException">Thrown if the localization module is not loaded.</exception>
        /// <exception cref="ArgumentException">Thrown if the term is already registered.</exception>
        public static void AddTerm(string term, string en, string cn = "")
        {
            AddTerm(term, new Dictionary<string, string> { { "en", en }, { "zh-CN", cn } });
        }

        /// Adds localization entries for the name and description of a specified entity in the game.
        /// <param name="obj">The unique identifier for the object whose localization is being added.</param>
        /// <param name="enName">The object's name in English.</param>
        /// <param name="enDesc">The object's description in English.</param>
        /// <param name="cnName">The object's name in Chinese (optional).</param>
        /// <param name="cnDesc">The object's description in Chinese (optional).</param>
        public static void AddEntityLocalization(ObjectID obj, string enName, string enDesc, string cnName = "",
            string cnDesc = "")
        {
            if (obj == ObjectID.None) return;

            AddTerm($"Items/{obj}", enName, cnName);
            AddTerm($"Items/{obj}Desc", enDesc, cnDesc);
        }

        /// Adds localization terms for the name and description of a specified entity, in multiple languages.
        /// <param name="objectName">The unique identifier of the entity.</param>
        /// <param name="enName">The name of the entity in English.</param>
        /// <param name="enDesc">The description of the entity in English.</param>
        /// <param name="cnName">The name of the entity in Chinese (optional).</param>
        /// <param name="cnDesc">The description of the entity in Chinese (optional).</param>
        public static void AddEntityLocalization(string objectName, string enName, string enDesc, string cnName = "",
            string cnDesc = "")
        {
            if (string.IsNullOrEmpty(objectName)) return;

            AddTerm($"Items/{objectName}", enName, cnName);
            AddTerm($"Items/{objectName}Desc", enDesc, cnDesc);
        }

        #endregion

        #region Internal Region

        internal static void ExtractTermsFromModdedLocalizationTables(LoadedMod mod)
        {
            var tableList = mod.Assets.OfType<ModdedLocalizationTable>();
            foreach (var table in tableList)
            {
                var localizationTerms = table.GetTerms();
                foreach (var term in localizationTerms)
                {
                    foreach (var lang in term.languageTerms)
                    {
                        AddTerm_Internal(term.term, lang);
                    }
                }
            }
        }
        
        internal static void AddTerm_Internal(string term, LanguageTerm translations)
        {
            if (string.IsNullOrEmpty(translations.translation)) return;
            localizationSourceData.AddLanguage(translations.LocalizationName, translations.LocalizationCode);
            if(!localizationSourceData.IsLanguageEnabled(translations.LocalizationName))
                localizationSourceData.EnableLanguage(translations.LocalizationName, true);
            int index = localizationSourceData.GetLanguageIndex(translations.LocalizationName);
            if (!localizationSourceData.ContainsTerm(term)) localizationSourceData.AddTerm(term);
            var termData = localizationSourceData.GetTermData(term);
            termData.SetTranslation(index, translations.translation);
            log.LogInfo($"Added localization term {term} with translation {translations.LocalizationCode} - {translations.translation}");
        }

        #endregion
    }
}