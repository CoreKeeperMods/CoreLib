using System;

namespace CoreLib.Submodules.JsonLoader
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RegisterReaderAttribute : Attribute
    {
        public string typeName;

        public RegisterReaderAttribute(string typeName)
        {
            this.typeName = typeName;
        }
    }
}