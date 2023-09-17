using CoreLib.Submodules.JsonLoader.Components;

namespace CoreLib.Submodules.JsonLoader
{
    public interface IInteractionHandler
    {
        void OnInteraction(TemplateBlock block);
    }

    public interface ITriggerListener
    {
        void OnTriggerEnter(TemplateBlock block);
        void OnTriggerExit(TemplateBlock block);
    }
}