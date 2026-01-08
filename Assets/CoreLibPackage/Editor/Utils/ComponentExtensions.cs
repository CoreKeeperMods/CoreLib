using UnityEditorInternal;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    public static class ComponentExtensions
    {
        /// <summary>Moves the specified component to the top of the Unity Inspector's component list.</summary>
        /// <param name="newComponent">The component to be repositioned at the top of the Inspector component hierarchy.</param>
        public static void MoveToTop(this UnityEngine.Component newComponent)
        {
            while (newComponent.GetComponentIndex() != 1)
            {
                ComponentUtility.MoveComponentUp(newComponent);
            }
        }
        
        /// <summary>Moves the specified component to the bottom of the Unity Inspector's component list.</summary>
        /// <param name="newComponent">The component to be repositioned at the bottom of the Inspector component hierarchy.</param>
        public static void MoveToBottom(this UnityEngine.Component newComponent)
        {
            while (newComponent.GetComponentIndex() != newComponent.gameObject.GetComponentCount() - 1)
            {
                ComponentUtility.MoveComponentDown(newComponent);
            }
        }
    }
}