using CoreLib.Submodules.JsonLoader.Components;

namespace CoreLib.Submodules.JsonLoader
{
    public interface IInteractionHandler
    {
        void OnInteract(TemplateBlock block);
        void OnEnter(TemplateBlock block);
        void OnExit(TemplateBlock block);
    }
}