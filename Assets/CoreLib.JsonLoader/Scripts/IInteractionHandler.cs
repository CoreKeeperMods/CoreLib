using CoreLib.JsonLoader.Components;

namespace CoreLib.JsonLoader
{
    public interface IInteractionHandler
    {
        void OnInteract(TemplateBlock block);
        void OnEnter(TemplateBlock block);
        void OnExit(TemplateBlock block);
    }
}