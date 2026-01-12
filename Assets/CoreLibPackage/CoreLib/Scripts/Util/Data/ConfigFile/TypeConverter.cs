using System;
//All code in this folder is from BepInEx library and is licensed under LGPL-2.1 license.

// ReSharper disable once CheckNamespace
namespace CoreLib.Data.Configuration
{
    ///     A serializer/deserializer combo for some type(s). Used by the config system.
    public class TypeConverter
    {
        ///     Used to serialize the type into a (hopefully) human-readable string.
        ///     Object is the instance to serialize, Type is the object's type.
        public Func<object, Type, string> ConvertToString { get; set; }

        ///     Used to deserialize the type from a string.
        ///     String is the data to deserialize, Type is the object's type, should return instance to an object of Type.
        public Func<string, Type, object> ConvertToObject { get; set; }
    }
}
