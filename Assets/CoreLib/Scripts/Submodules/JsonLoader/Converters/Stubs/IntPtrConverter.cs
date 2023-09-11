﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreLib.Submodules.JsonLoader.Converters
{
    public class IntPtrConverter : JsonConverter<IntPtr>
    {
        public override IntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return IntPtr.Zero;
        }

        public override void Write(Utf8JsonWriter writer, IntPtr value, JsonSerializerOptions options)
        {
            writer.WriteNullValue();
        }
    }
}