using System;

namespace CoreLib.Submodules.ModSystem
{
    [AttributeUsage(AttributeTargets.Class)]
    public class InstallInAttribute : Attribute
    {
        public enum Target
        {
            Client,
            Server,
            Both
        }

        public Target target;

        public InstallInAttribute(Target target)
        {
            this.target = target;
        }
    }
}