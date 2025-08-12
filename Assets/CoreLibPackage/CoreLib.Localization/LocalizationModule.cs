using System;
using System.Collections.Generic;
using CoreLib.Localization.Patches;
using I2.Loc;

namespace CoreLib.Localization
{
    /// <summary>
    /// Provides functionality for managing localization strings and entity localization within the game.
    /// </summary>
    public class LocalizationModule : BaseSubmodule
    {
        #region Public Interface

        /// <summary>
        /// Add a new localization term with specified translations.
        /// </summary>
        /// <param name="term">Unique identifier for the localization term.</param>
        /// <param name="translations">Dictionary containing translations for each language.</param>
        /// <exception cref="ArgumentException">Thrown if the translation dictionary does not include an English ("en") translation or if the term is already registered.</exception>
        public static void AddTerm(string term, Dictionary<string, string> translations)
        {
            Instance.ThrowIfNotLoaded();

            if (!translations.ContainsKey("en"))
                throw new ArgumentException("Translation dictionary must contain english translation!");

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
        /// Adds a new localization term with English and optional Chinese translations.
        /// </summary>
        /// <param name="term">A unique identifier for the localization term.</param>
        /// <param name="en">The English translation of the term.</param>
        /// <param name="cn">The optional Chinese translation of the term. Defaults to an empty string.</param>
        /// <exception cref="InvalidOperationException">Thrown if the localization module is not loaded.</exception>
        /// <exception cref="ArgumentException">Thrown if the term is already registered.</exception>
        public static void AddTerm(string term, string en, string cn = "")
        {
            Instance.ThrowIfNotLoaded();

            AddTerm(term, new Dictionary<string, string> { { "en", en }, { "zh-CN", cn } });
        }

        /// <summary>
        /// Adds localization entries for the name and description of a specified entity in the game.
        /// </summary>
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

        /// <summary>
        /// Adds localization terms for the name and description of a specified entity, in multiple languages.
        /// </summary>
        /// <param name="obj">The unique identifier of the entity.</param>
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

        #region Private Implementation

        internal override GameVersion Build => new GameVersion(1, 1, 0, "90bc");

        /// <summary>
        /// Gets the version of the localization module.
        /// </summary>
        /// <remarks>
        /// The property returns the version of the LocalizationModule as a string.
        /// This value is specific to the module's implementation and can be used to
        /// identify the module's build or compatibility state.
        /// </remarks>
        internal override string Version => "3.1.1";

        /// <summary>
        /// Gets the singleton instance of the <see cref="LocalizationModule"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="Instance"/> property provides access to the single, centralized
        /// instance of the <see cref="LocalizationModule"/> class used for managing localization
        /// functionality and operations throughout the application. It ensures efficient and
        /// consistent access to the module's functionality while adhering to the singleton design pattern.
        /// </remarks>
        internal static LocalizationModule Instance => CoreLibMod.GetModuleInstance<LocalizationModule>();

        /// <summary>
        /// Implements hooks necessary for integrating the submodule's functionality into the application framework.
        /// </summary>
        /// <remarks>
        /// This method is called internally to apply patches or modifications utilizing the CoreLibMod infrastructure.
        /// It is intended to override the base implementation to provide module-specific behavior.
        /// </remarks>
        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(TextManager_Patch));
        }

        /// <summary>
        /// A static dictionary that stores localization terms and their corresponding translations.
        /// </summary>
        /// <remarks>
        /// This dictionary maps a term (as a string key) to another dictionary containing relevant translations.
        /// The inner dictionary uses language codes (e.g., "en" for English, "fr" for French) as keys and localized strings as values.
        /// It is primarily used to accumulate translation entries dynamically before the localization system is fully initialized.
        /// Once the system is initialized, the entries are transferred and incorporated into the main localization framework.
        /// </remarks>
        internal static Dictionary<string, Dictionary<string, string>> addedTranslations =
            new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Indicates whether the localization system is fully initialized and ready to handle localization terms and translations.
        /// </summary>
        /// <remarks>
        /// This variable is set to <c>true</c> when the localization system has been successfully initialized and all pre-added
        /// translations have been processed. While it is <c>false</c>, new terms and translations may be queued or stored temporarily
        /// until the system becomes fully operational.
        /// </remarks>
        internal static bool localizationSystemReady;

        #endregion
    }
}