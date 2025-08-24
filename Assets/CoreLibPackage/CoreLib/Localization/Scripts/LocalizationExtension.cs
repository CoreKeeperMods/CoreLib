using System.Collections.Generic;
using I2.Loc;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Localization
{
    /// <summary>
    /// A static class providing extension methods for language source data manipulation
    /// related to localization functionalities.
    /// </summary>
    internal static class LocalizationExtension
    {
        /// <summary>
        /// Adds a new term with its translations to the language source if it does not already exist.
        /// </summary>
        /// <param name="source">The language source data where the term will be added.</param>
        /// <param name="term">The term to be added.</param>
        /// <param name="translations">A dictionary containing translations for the term, keyed by language codes.</param>
        internal static void AddTerm(this LanguageSourceData source, string term, Dictionary<string, string> translations)
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

            List<string> languages = new List<string>(source.mLanguages.Count);
            foreach (LanguageData data in source.mLanguages)
            {
                languages.Add(translations.ContainsKey(data.Code) ? translations[data.Code] : translations["en"]);
            }

            termdata.Languages = languages.ToArray();
            source.mDictionary.Add(termdata.Term, termdata);
            source.mTerms.Add(termdata);
        }
    }
}