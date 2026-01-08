using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Util.Extension;
using I2.Loc;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Localization
{
    [CreateAssetMenu(menuName = "CoreLib/Localization/Localization Table", fileName = "Localization Table"), Serializable]
    public class ModdedLocalizationTable : ScriptableObject
    {
        [Header("Localization Table Settings")]
        public List<LocalizationTerm> terms = new();
        [HideInInspector] public LanguageSourceData sourceData = new();

        private void OnValidate()
        {
            sourceData.ClearAllData();
            foreach (var term in terms)
            {
                foreach (var languageTerm in term.languageTerms)
                {
                    sourceData.AddLanguage(languageTerm.LocalizationName, languageTerm.LocalizationCode);
                    if (!sourceData.ContainsTerm(term.term)) sourceData.AddTerm(term.term);
                    var termData = sourceData.GetTermData(term.term);
                    termData.SetTranslation(sourceData.GetLanguageIndex(languageTerm.LocalizationName), languageTerm.translation);
                }
            }
        }

        public List<LocalizationTerm> GetTerms()
        {
            var termList = new List<LocalizationTerm>();
            foreach (var term in sourceData.mTerms)
            {
                termList.Add(new LocalizationTerm
                {
                    term = term.Term,
                    languageTerms = term.Languages.Select((translation, i) => new LanguageTerm
                    {
                        localizationCodeDropdown = GoogleLanguages.GetLanguagesForDropdown("","")
                            .First(str => str.Split('[').Last().Contains(sourceData.mLanguages[i].Code)),
                        translation = translation
                    }).ToList()
                });
            }
            return termList;
        }
    }
    
    [Serializable]
    public class LocalizationTerm
    {
        public string term;
        public List<LanguageTerm> languageTerms = new();
    }

    [Serializable]
    public class LanguageTerm
    {
        [GoogleLanguagesDropdown]
        public string localizationCodeDropdown;
        public string LocalizationName => GoogleLanguages.GetFormatedLanguageName(localizationCodeDropdown);
        public string LocalizationCode => GoogleLanguages.GetLanguageCode(LocalizationName);
        public string translation;
    }
}