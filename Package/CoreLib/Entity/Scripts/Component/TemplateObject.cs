using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Component
{
    /// Represents a customizable template object
    public class TemplateObject : MonoBehaviour
    {
        /// Specifies the starting quantity or initial count of an object.
        public int initialAmount = 1;

        /// Represents the variation or distinct attribute of an object.
        public int variation;

        /// Indicates whether the variation of an object is dynamic.
        public bool variationIsDynamic;

        /// Represents the specific variation to switch or toggle to.
        public int variationToToggleTo;

        /// Specifies the category or classification of the object.
        public ObjectType objectType;

        /// Represents a collection of tags associated with the object.
        public List<ObjectCategoryTag> tags;

        /// Defines the rarity level of the object
        public Rarity rarity;

        /// Represents the graphical representation of the object as a prefab.
        public GameObject graphicalPrefab;
        public DataBlockRef<GraphicalObjectDataBlock> graphicalRef;

        /// A list of supplementary sprites associated with the object.
        public List<Sprite> additionalSprites;

        /// Converts the current instance of TemplateObject into an ObjectAuthoring instance.
        /// <returns>Returns the newly created and initialized ObjectAuthoring instance.</returns>
        public ObjectAuthoring Convert()
        {
            if (!gameObject.TryGetComponent(out ObjectAuthoring objectAuthoring))
                objectAuthoring = gameObject.AddComponent<ObjectAuthoring>();
            objectAuthoring.initialAmount = initialAmount;
            objectAuthoring.variation = variation;
            objectAuthoring.variationIsDynamic = variationIsDynamic;
            objectAuthoring.variationToToggleTo = variationToToggleTo;
            objectAuthoring.objectType = objectType;
            objectAuthoring.tags = tags;
            objectAuthoring.rarity = rarity;
            objectAuthoring.graphicalPrefab = graphicalPrefab;
            objectAuthoring.graphicalRef = graphicalRef;
            objectAuthoring.additionalSprites = additionalSprites;
            return objectAuthoring;
        }
    }
}