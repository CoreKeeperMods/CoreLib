using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Components
{
    /// <summary>
    /// Represents a customizable template object within the game environment.
    /// Used for defining initial properties, attributes, and configuration of the object.
    /// </summary>
    public class TemplateObject : MonoBehaviour
    {
        /// <summary>
        /// Specifies the starting quantity or initial count of an object.
        /// This value determines the default amount allocated or available
        /// at the beginning of its lifecycle or usage.
        /// </summary>
        public int initialAmount = 1;

        /// <summary>
        /// Represents the variation or distinct attribute of an object.
        /// This value can define a specific configuration, state, or
        /// property of the object, contributing to its uniqueness or behavior.
        /// </summary>
        public int variation;

        /// <summary>
        /// Indicates whether the variation of an object is dynamic.
        /// When set to true, this property suggests that the object's variation
        /// can change or adapt at runtime, allowing for more flexible behavior
        /// or configuration adjustments.
        /// </summary>
        public bool variationIsDynamic;

        /// <summary>
        /// Represents the specific variation to switch or toggle to.
        /// This value is typically used to define a target configuration,
        /// state, or version of an object within the context of dynamic
        /// adjustments, variation changes, or customization.
        /// </summary>
        public int variationToToggleTo;

        /// <summary>
        /// Specifies the category or classification of the object.
        /// This property determines the general type or role of the object
        /// within the game or application, and it can be used for logic,
        /// organization, or behavior differentiation.
        /// </summary>
        public ObjectType objectType;

        /// <summary>
        /// Represents a collection of tags associated with the object.
        /// Tags are used to provide metadata or categorization, which can help
        /// in filtering, grouping, or defining specific characteristics of the object.
        /// </summary>
        public List<ObjectCategoryTag> tags;

        /// <summary>
        /// Defines the rarity level of the object, which can be used
        /// to categorize or differentiate objects based on their value,
        /// scarcity, or significance within the game environment.
        /// </summary>
        public Rarity rarity;

        /// <summary>
        /// Represents the graphical representation of the object as a prefab.
        /// This prefab can be instantiated or referenced to visually depict the object
        /// in the game environment.
        /// </summary>
        public GameObject graphicalPrefab;

        /// <summary>
        /// A list of supplementary sprites associated with the object.
        /// These sprites can be used for additional graphical variations or dynamic visual representations.
        /// </summary>
        public List<Sprite> additionalSprites;

        /// Converts the current instance of TemplateObject into an ObjectAuthoring instance.
        /// This method transfers the data from TemplateObject properties to a new ObjectAuthoring
        /// component attached to the same GameObject, then destroys the TemplateObject component.
        /// <returns>
        /// Returns the newly created and initialized ObjectAuthoring instance.
        /// </returns>
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