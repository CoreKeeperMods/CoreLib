namespace CoreLib.Submodules.CustomEntity.Interfaces
{
    public interface IDynamicItemHandler
    {
        bool ShouldApply(ObjectDataCD objectData);

        void ApplyText(ObjectDataCD objectData, TextAndFormatFields text);
        bool ApplyColors(ObjectDataCD objectData, ColorReplacementData colorData);
    }
}