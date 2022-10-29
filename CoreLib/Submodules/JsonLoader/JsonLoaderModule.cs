using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using CoreLib.Submodules.CustomEntity;
using CoreLib.Submodules.JsonLoader.Converters;
using CoreLib.Submodules.JsonLoader.Readers;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using FieldInfo = Il2CppSystem.Reflection.FieldInfo;
using Object = Il2CppSystem.Object;

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
            RegisterJsonReaders(Assembly.GetExecutingAssembly());

            options = new JsonSerializerOptions
            {
                IncludeFields = true
            };
            options.Converters.Add(new ObjectIDConverter());
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            options.Converters.Add(new Il2CppListConverter());
            options.Converters.Add(new SpriteConverter());
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
        public static string context;

        public static Dictionary<string, IJsonReader> jsonReaders = new Dictionary<string, IJsonReader>();
        public static Dictionary<string, string> modFolders = new Dictionary<string, string>();

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
            context = resourcesDir;

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

            context = "";
            modFolders.Add(modGuid, path);
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
            }else if (fieldInfo != null)
            {
                var parsedValue = updatedProperty.Value.Deserialize(fieldInfo.FieldType, options);

                fieldInfo.SetValue(target, parsedValue);
            }
            else
            {
                CoreLibPlugin.Logger.LogInfo($"Found no property/field named {updatedProperty.Key} in {type.FullName}");
            }
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
    }
}