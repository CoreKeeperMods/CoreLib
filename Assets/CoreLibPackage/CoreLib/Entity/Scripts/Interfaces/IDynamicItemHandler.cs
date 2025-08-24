// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Interfaces
{
    /// <summary>
    /// Represents an interface for handling dynamic items by altering their properties,
    /// such as text and visuals, based on specified object data.
    /// </summary>
    public interface IDynamicItemHandler
    {
        /// <summary>
        /// Determines whether this handler should be applied for the specified item data.
        /// </summary>
        /// <param name="objectData">The data associated with the object being evaluated.</param>
        /// <returns>True if the handler should apply to the specified object; otherwise, false.</returns>
        bool ShouldApply(ObjectDataCD objectData);

        /// <summary>
        /// Apply dynamic text to the item.
        /// </summary>
        /// <param name="objectData">The object data to determine the text modification.</param>
        /// <param name="text">The text and format fields to be modified based on the object data.</param>
        void ApplyText(ObjectDataCD objectData, TextAndFormatFields text);

        /// <summary>
        /// Applies dynamic recoloring to the item based on the provided object data and color replacement data.
        /// </summary>
        /// <param name="objectData">The object data used to determine applicable colors.</param>
        /// <param name="colorData">The color replacement data to apply to the item.</param>
        /// <returns>True if the recoloring is applied; otherwise, false.</returns>
        bool ApplyColors(ObjectDataCD objectData, ColorReplacementData colorData);
    }
}