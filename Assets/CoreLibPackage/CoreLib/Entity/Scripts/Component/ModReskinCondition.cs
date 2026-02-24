using System.Collections.Generic;
using CoreLib.Util.Extension;
using NaughtyAttributes;
using Pug.UnityExtensions;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Component
{
    [DisallowMultipleComponent]
    public class ModReskinCondition : MonoBehaviour
    {
        public ObjectID EntityID => this.GetEntityObjectID();
        public GameObject BuildingPrefab
        {
            get { 
                if(TryGetComponent(out TemplateObject templateObject))
                    return templateObject.graphicalPrefab;
                return TryGetComponent(out ObjectAuthoring objectAuthoring) ? objectAuthoring.graphicalPrefab : null;
            }
        }

        
        
        [FormerlySerializedAs("DependsOnVariation")]
        [InfoBox("The sprites are re-skinned based on the first matching entry." +
                 "If none match, the default skin is used. Each entry in the reskin list applies to the corresponding sprite in the spritesToReskin list.")]
        [Header("Reskin Condition")]
        [Tooltip("If true, the reskin condition depends on the variation of the building.")]
        public bool dependsOnVariation;
        [AllowNesting, ShowIf("dependsOnVariation"), Tooltip("The variation to match. If none match, the default variation is used.")]
        public int variation;
        [Tooltip("None matches any season.")]
        public Season season;
        [Tooltip("If null, the sprite's corresponding default is used.")]
        public List<SpriteSkinFromEntityAndSeason.SkinAndGradientMap> reskin = new();

        public SpriteSkinFromEntityAndSeason.ReskinCondition GetReskinCondition() =>
            new()
            {
                objectID = EntityID,
                dependsOnVariation = dependsOnVariation,
                variation = variation,
                season = season,
                reskin = reskin
            };

        public void OnValidate()
        {
            if (BuildingPrefab == null || !BuildingPrefab.TryGetComponent(out SpriteSkinFromEntityAndSeason skin))
            {
                reskin.Clear();
                return;
            }
            var elementToFillOutWith = reskin.Count <= 0 ? new SpriteSkinFromEntityAndSeason.SkinAndGradientMap() : reskin[^1];
            int count = skin.spritesToReskin.Count;
            reskin.Resize(elementToFillOutWith, count);
        }
    }
}