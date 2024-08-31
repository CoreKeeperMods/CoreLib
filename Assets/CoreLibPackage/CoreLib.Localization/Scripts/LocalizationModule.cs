using System;
using System.Collections.Generic;
using CoreLib.Localization.Patches;
using CoreLib.Util.Extensions;
using I2.Loc;

namespace CoreLib.Localization
{
    /// <summary>
    /// This modules provides means to add localization strings to the game
    /// </summary>
    public class LocalizationModule : BaseSubmodule
    {

        #region Public Interface

        /// <summary>
        /// Add new localization term
        /// </summary>
        /// <param name="term">UNIQUE term id</param>
        /// <param name="translations">dictionary with translations for each language</param>
        /// <exception cref="ArgumentException">thrown if english translation is not specified</exception>
        public static void AddTerm(string term, Dictionary<string, string> translations)
        {
            Instance.ThrowIfNotLoaded();
        
            if (!translations.ContainsKey("en")) throw new ArgumentException("Translation dictionary must contain english translation!");

            if (addedTranslations.ContainsKey(term))
            {
                throw new ArgumentException($"Term {term} is already registered!");
            }

            if (!localizationSystemReady)
            {
                addedTranslations.Add(term, translations);
            }
            else
            {
                LanguageSourceData source = LocalizationManager.Sources[0];
                source.AddTerm(term, translations);
                source.UpdateDictionary();
            }
        }

        /// <summary>
        /// Short form to add localization term
        /// </summary>
        /// <param name="term">UNIQUE term id</param>
        /// <param name="en">English translation</param>
        /// <param name="cn">Chinese translation</param>
        public static void AddTerm(string term, string en, string cn = "")
        {
            Instance.ThrowIfNotLoaded();
        
            AddTerm(term, new Dictionary<string, string> { { "en", en }, { "zh-CN", cn } });
        }
    
        /// <summary>
        /// Add I2 terms for entity name and description
        /// </summary>
        /// <param name="enName">Object name in English</param>
        /// <param name="enDesc">Object description in English</param>
        /// <param name="cnName">Object name in Chinese</param>
        /// <param name="cnDesc">Object description in Chinese</param>
        public static void AddEntityLocalization(ObjectID obj, string enName, string enDesc, string cnName = "", string cnDesc = "")
        {
            if (obj == ObjectID.None) return;
        
            AddTerm($"Items/{obj}", enName, cnName);
            AddTerm($"Items/{obj}Desc", enDesc, cnDesc);
        }
        
        /// <summary>
        /// Add I2 terms for entity name and description
        /// </summary>
        /// <param name="enName">Object name in English</param>
        /// <param name="enDesc">Object description in English</param>
        /// <param name="cnName">Object name in Chinese</param>
        /// <param name="cnDesc">Object description in Chinese</param>
        public static void AddEntityLocalization(string objectName, string enName, string enDesc, string cnName = "", string cnDesc = "")
        {
            if (string.IsNullOrEmpty(objectName)) return;
        
            AddTerm($"Items/{objectName}", enName, cnName);
            AddTerm($"Items/{objectName}Desc", enDesc, cnDesc);
        }
    
        #endregion

        #region Private Implementation

        internal override GameVersion Build => new GameVersion(1, 0, 0, "4407");
        internal override string Version => "3.1.1";
        internal static LocalizationModule Instance => CoreLibMod.GetModuleInstance<LocalizationModule>();

        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(TextManager_Patch));
        }

        internal static Dictionary<string, Dictionary<string, string>> addedTranslations = new Dictionary<string, Dictionary<string, string>>();
        internal static bool localizationSystemReady;

        #endregion
    }
}