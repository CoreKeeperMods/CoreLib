using System;
using System.Collections.Generic;
using I2.Loc;

namespace CoreLib;

public static class Localization
{
    internal static Dictionary<string, Dictionary<string, string>> addedTranslations = new Dictionary<string, Dictionary<string, string>>();
    internal static bool localizationSystemReady;
    
    internal static void AddTerm(this LanguageSourceData source, string term, Dictionary<string, string> translations)
    {
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
    
    /// <summary>
    /// Add new localization term
    /// </summary>
    /// <param name="term">UNIQUE term id</param>
    /// <param name="translations">dictionary with translations for each language</param>
    /// <exception cref="ArgumentException">thrown if english translation is not specified</exception>
    public static void AddTerm(string term, Dictionary<string, string> translations)
    {
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
        AddTerm(term, new Dictionary<string, string> {{"en", en}, {"zh-CN", cn}});
    }
}