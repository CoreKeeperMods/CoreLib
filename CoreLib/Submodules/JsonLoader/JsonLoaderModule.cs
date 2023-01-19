using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CoreLib.Components;
using CoreLib.Submodules.CustomEntity;
using CoreLib.Submodules.JsonLoader.Converters;
using CoreLib.Submodules.JsonLoader.Readers;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;

namespace CoreLib.Submodules.JsonLoader
{
    [CoreLibSubmodule(Dependencies = new[] { typeof(CustomEntityModule) })]
    public class JsonLoaderModule
    {
        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded
        {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() { }


        [CoreLibSubmoduleInit(Stage = InitStage.Load)]
        internal static void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<TemplateBlock>();
            RegisterJsonReaders(Assembly.GetExecutingAssembly());

            options = new JsonSerializerOptions
            {
                IncludeFields = true
            };
            options.Converters.Add(new ObjectTypeConverter());
            options.Converters.Add(new ObjectIDConverter());
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new Il2CppListConverter());
            options.Converters.Add(new SpriteConverter());
            options.Converters.Add(new ColorConverter());
        }

        [CoreLibSubmoduleInit(Stage = InitStage.PostLoad)]
        internal static void PostLoad()
        {
            CustomEntityModule.RegisterECSComponent<TemplateBlockCD>();
            CustomEntityModule.RegisterECSComponent<TemplateBlockCDAuthoring>();
        }

        internal static void ThrowIfNotLoaded()
        {
            if (!Loaded)
            {
                Type submoduleType = MethodBase.GetCurrentMethod().DeclaringType;
                string message = $"{submoduleType.Name} is not loaded. Please use [{nameof(CoreLibSubmoduleDependency)}(nameof({submoduleType.Name})]";
                throw new InvalidOperationException(message);
            }
        }

        public static JsonSerializerOptions options;
        public static string context = "";

        public static Dictionary<string, IJsonReader> jsonReaders = new Dictionary<string, IJsonReader>();
        public static Dictionary<string, string> modFolders = new Dictionary<string, string>();

        public static IDisposable WithContext(string path)
        {
            return new ContextHandle(path);
        }

        public static void LoadFolder(string modGuid, string path)
        {
            ThrowIfNotLoaded();

            if (modFolders.ContainsKey(modGuid))
            {
                CoreLibPlugin.Logger.LogWarning($"Trying to load mod {modGuid} folder again!");
                return;
            }

            if (!Directory.Exists(Path.Combine(path, "resources")))
            {
                CoreLibPlugin.Logger.LogWarning($"Mod {modGuid} folder does not contain 'resources' folder!");
                return;
            }

            string resourcesDir = Path.Combine(path, "resources");
            using (WithContext(resourcesDir))
            {
                List<JsonNode> objectsCache = new List<JsonNode>();

                foreach (string file in Directory.EnumerateFiles(resourcesDir, "*.json", SearchOption.AllDirectories))
                {
                    string filename = file.Substring(file.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                    JsonNode jObject = JsonNode.Parse(File.ReadAllText(file));

                    if (jObject["type"] == null)
                    {
                        CoreLibPlugin.Logger.LogWarning(
                            $"JSON definition file {Path.GetRelativePath(resourcesDir, file)} does not contain type information! Please specify 'type' value!");
                        continue;
                    }

                    string type = jObject["type"].GetValue<string>();

                    if (jsonReaders.ContainsKey(type))
                    {
                        try
                        {
                            CoreLibPlugin.Logger.LogInfo($"Loading JSON file {filename} with {type} reader.");
                            IJsonReader reader = jsonReaders[type];
                            reader.ApplyPre(jObject);
                            jObject["filename"] = filename;
                            objectsCache.Add(jObject);
                        }
                        catch (Exception e)
                        {
                            CoreLibPlugin.Logger.LogWarning($"Failed to add object:\n{e}");
                        }
                    }
                }

                foreach (JsonNode jObject in objectsCache)
                {
                    string type = jObject["type"].GetValue<string>();

                    if (jsonReaders.ContainsKey(type))
                    {
                        try
                        {
                            CoreLibPlugin.Logger.LogInfo($"Post Loading JSON file {jObject["filename"].GetValue<string>()} with {type} reader.");
                            IJsonReader reader = jsonReaders[type];
                            reader.ApplyPost(jObject);
                        }
                        catch (Exception e)
                        {
                            CoreLibPlugin.Logger.LogWarning($"Failed to post load:\n{e}");
                        }
                    }
                }
                modFolders.Add(modGuid, path);
            }
        }

        public static void RegisterJsonReaders(Assembly assembly)
        {
            IEnumerable<Type> types = assembly
                .GetTypes()
                .Where(type => type.GetCustomAttribute<RegisterReaderAttribute>() != null);

            foreach (Type type in types)
            {
                RegisterJsonReadersInType_Internal(type);
            }
        }

        private static void RegisterJsonReadersInType_Internal(Type type)
        {
            RegisterReaderAttribute attribute = type.GetCustomAttribute<RegisterReaderAttribute>();
            if (!jsonReaders.ContainsKey(attribute.typeName))
            {
                IJsonReader reader = Activator.CreateInstance(type) as IJsonReader;
                jsonReaders.Add(attribute.typeName, reader);
            }
            else
            {
                CoreLibPlugin.Logger.LogWarning($"Failed to register {type.FullName} Json Reader, because name {attribute.typeName} is already taken!");
            }
        }

        public static void PopulateObject<T>(T target, JsonNode jsonSource)
        {
            PopulateObject(typeof(T), target, jsonSource);
        }

        public static void PopulateObject(Type type, object target, JsonNode jsonSource)
        {
            foreach (KeyValuePair<string, JsonNode> property in jsonSource.AsObject())
            {
                CoreLibPlugin.Logger.LogInfo($"Overriding {property.Key} in {target.GetType().FullName}");
                OverwriteProperty(type, target, property);
            }
        }

        private static void OverwriteProperty(Type type, object target, KeyValuePair<string, JsonNode> updatedProperty)
        {
            var propertyInfo = type.GetProperty(updatedProperty.Key);
            var fieldInfo = type.GetField(updatedProperty.Key);

            if (propertyInfo != null)
            {
                var parsedValue = updatedProperty.Value.Deserialize(propertyInfo.PropertyType, options);

                propertyInfo.SetValue(target, parsedValue);
            }
            else if (fieldInfo != null)
            {
                if (IsIl2CppField(fieldInfo.FieldType, out Type systemType))
                {
                    var parsedValue = updatedProperty.Value.Deserialize(systemType, options);
                    object il2cppField = fieldInfo.GetValue(target);
                    var property = fieldInfo.FieldType.GetProperty("Value");
                    property.SetValue(il2cppField, parsedValue);
                }
                else
                {
                    var parsedValue = updatedProperty.Value.Deserialize(fieldInfo.FieldType, options);
                    fieldInfo.SetValue(target, parsedValue);
                }
            }
            else
            {
                CoreLibPlugin.Logger.LogInfo($"Found no property/field named {updatedProperty.Key} in {type.FullName}");
            }
        }

        public static bool IsIl2CppField(Type type, out Type systemType)
        {
            systemType = default;

            if (type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof(Il2CppReferenceField<>) ||
                 type.GetGenericTypeDefinition() == typeof(Il2CppValueField<>)))
            {
                systemType = type.GetGenericArguments()[0];
                return true;
            }

            if (type == typeof(Il2CppStringField))
            {
                systemType = typeof(string);
                return true;
            }

            return false;
        }

        public static void FillArrays<T>(T target)
        {
            FillArrays(typeof(T), target);
        }
        
        public static void FillArrays(Type type, object target)
        {
            foreach (PropertyInfo property in type.GetProperties())
            {
                if (!property.PropertyType.IsGenericType) continue;

                if (property.PropertyType.GetGenericTypeDefinition() == typeof(Il2CppSystem.Collections.Generic.List<>))
                {
                    object value = property.GetValue(target);
                    if (value == null)
                    {
                        property.SetValue(target, Activator.CreateInstance(property.PropertyType));
                    }
                }
            }
        }

        private static Type[] GetTypesFromAssembly(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null).ToArray();
            }
        }

        private static IEnumerable<Type> AllTypes() => AccessTools.AllAssemblies().SelectMany(GetTypesFromAssembly);

        public static Type TypeByName(string name)
        {
            Type type = Type.GetType(name, false);

            type ??= AllTypes().FirstOrDefault(t => t.FullName == name);
            type ??= AllTypes().FirstOrDefault(t => t.Name == name);

            if (type == null)
                CoreLibPlugin.Logger.LogWarning($"Could not find type named {name}");
            return type;
        }
    }
}