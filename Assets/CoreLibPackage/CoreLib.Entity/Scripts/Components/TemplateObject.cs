using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Components
{
    public class TemplateObject : MonoBehaviour
    {
        public int initialAmount = 1;
        public int variation;
        public bool variationIsDynamic;
        public int variationToToggleTo;
        public ObjectType objectType;
        public List<ObjectCategoryTag> tags;
        public Rarity rarity;
        public GameObject graphicalPrefab;
        public List<Sprite> additionalSprites;

        public ObjectAuthoring Convert()
        {
            var objectAuthoring = gameObject.AddComponent<ObjectAuthoring>();
            objectAuthoring.initialAmount = initialAmount;
            objectAuthoring.variation = variation;
            objectAuthoring.variationIsDynamic = variationIsDynamic;
            objectAuthoring.variationToToggleTo = variationToToggleTo;
            objectAuthoring.objectType = objectType;
            objectAuthoring.tags = tags;
            objectAuthoring.rarity = rarity;
            objectAuthoring.graphicalPrefab = graphicalPrefab;
            objectAuthoring.additionalSprites = additionalSprites;
            Destroy(this);

            return objectAuthoring;
        }
    }
}