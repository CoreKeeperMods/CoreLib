namespace CoreLib.UserInterface.Util
{
    public static class ModUIExtensions
    {
        public static bool IsVisible<T>(this T modInterface)
            where T : IModUI
        {
            return modInterface.IsVisible();
        }
    }
}