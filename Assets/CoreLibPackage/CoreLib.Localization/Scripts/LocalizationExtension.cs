using System.Collections.Generic;
using I2.Loc;

namespace CoreLib.Localization
{
    internal static class LocalizationExtension
    {
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