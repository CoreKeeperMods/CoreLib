using System;
using JetBrains.Annotations;

namespace CoreLib.JsonLoader
{
    [AttributeUsage(AttributeTargets.Class)]
    [MeansImplicitUse]
    public class RegisterReaderAttribute : Attribute
    {
        public string typeName;

        public RegisterReaderAttribute(string typeName)
        {
            this.typeName = typeName;
        }
    }
}