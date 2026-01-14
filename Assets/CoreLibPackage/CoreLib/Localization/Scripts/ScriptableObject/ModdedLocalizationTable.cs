using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Util.Extension;
using I2.Loc;
using NaughtyAttributes;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Localization
{
    //TODO Remove this class and Localization Module?
    [CreateAssetMenu(menuName = "CoreLib/Localization/Localization Table", fileName = "Localization Table"), Serializable]
    public class ModdedLocalizationTable : ScriptableObject
    {
        [Header("Localization Table Settings")]
        public List<LocalizationTerm> terms = new();
        [HideInInspector] public LanguageSourceData sourceData = new();

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
        [OnValueChanged("ValidateTermChange"), AllowNesting]
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