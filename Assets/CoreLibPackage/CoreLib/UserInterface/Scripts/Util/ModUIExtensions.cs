using CoreLib.Submodule.UserInterface.Interface;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.UserInterface.Util
{
    /// <summary>
    /// Provides extension methods for mod user interface operations in the application.
    /// </summary>
    public static class ModUIExtensions
    {
        /// <summary>
        /// Determines whether the specified mod user interface is currently visible.
        /// </summary>
        /// <typeparam name="T">The type of the mod user interface, which must implement the IModUI interface.</typeparam>
        /// <param name="modInterface">The instance of the mod user interface to check for visibility.</param>
        /// <returns>True if the mod user interface is visible, otherwise false.</returns>
        public static bool IsVisible<T>(this T modInterface)
            where T : IModUI
        {
            return modInterface.IsVisible();
        }
    }
}