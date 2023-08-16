namespace CoreLib.Submodules.Entity.Interfaces
{
    /// <summary>
    /// Define Dynamic Item Handler by implementing this interface
    /// Dynamic Item Handlers can change item visual and text on the fly
    /// </summary>
    public interface IDynamicItemHandler
    {
        /// <summary>
        /// Determine whether this handler should be used for this item
        /// </summary>
        bool ShouldApply(ObjectDataCD objectData);

        /// <summary>
        /// Apply dynamic text to the item
        /// </summary>
        void ApplyText(ObjectDataCD objectData, TextAndFormatFields text);
        
        /// <summary>
        /// Apply dynamic recoloring to the item
        /// </summary>
        /// <param name="objectData"></param>
        /// <param name="colorData"></param>
        /// <returns></returns>
        bool ApplyColors(ObjectDataCD objectData, ColorReplacementData colorData);
    }
}