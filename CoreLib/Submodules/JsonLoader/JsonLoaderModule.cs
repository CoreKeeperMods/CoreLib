using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CoreLib.Components;
using CoreLib.Submodules.ChatCommands;
using CoreLib.Submodules.DropTables;
using CoreLib.Submodules.ModComponent;
using CoreLib.Submodules.ModEntity;
using CoreLib.Submodules.JsonLoader.Converters;
using CoreLib.Submodules.JsonLoader.Readers;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;

namespace CoreLib.Submodules.JsonLoader
{
    [CoreLibSubmodule(Dependencies = new[] { typeof(EntityModule), typeof(ComponentModule), typeof(DropTablesModule) })]
    public class JsonLoaderModule
    {
        #region PUBLIC_INTERFACE

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded
        {
            get => _loaded;
            internal set => _loaded = value;
        }

        public static void UseConverter(params JsonConverter[] converters)
        {
            foreach (JsonConverter converter in converters)
            {
                if (options.Converters.All(jsonConverter => jsonConverter.GetType() != converter.GetType()))
                {
                    options.Converters.Add(converter);
                }
            }
        }

        public static IDisposable WithContext(JsonContext path)
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

            IL2CPP.il2cpp_gc_disable();
            string resourcesDir = Path.Combine(path, "resources");

            using (WithContext(new JsonContext(resourcesDir, Assembly.GetCallingAssembly())))
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

            IL2CPP.il2cpp_gc_enable();
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

        public static int RegisterInteractHandler(string handlerType)
        {
            if (context.callingAssembly == null)
            {
                CoreLibPlugin.Logger.LogError("Failed to register interaction handler. Context assembly is null");
                return 0;
            }

            Type type = context.callingAssembly.GetType(handlerType);
            if (type == null)
            {
                CoreLibPlugin.Logger.LogError($"Failed to register interaction handler. Type '{handlerType}' not found!");
                return 0;
            }

            if (!type.IsAssignableTo(typeof(IInteractionHandler)) &&
                !type.IsAssignableTo(typeof(ITriggerListener)))
            {
                CoreLibPlugin.Logger.LogError(
                    $"Failed to register interaction handler. Type {handlerType} does not implement '{nameof(IInteractionHandler)}' or '{nameof(ITriggerListener)}'!");
                return 0;
            }

            int existingMethod = interactionHandlers.FindIndex(info => info != null && info.GetType() == type);

            if (existingMethod > 0)
            {
                return existingMethod;
            }

            CoreLibPlugin.Logger.LogDebug($"Registering {handlerType} as object interact handler!");
            int index = interactionHandlers.Count;
            interactionHandlers.Add(Activator.CreateInstance(type));
            return index;
        }

        public static Type TypeByName(string name)
        {
            Type type = Type.GetType(name, false);

            type ??= AllTypes().FirstOrDefault(t => t.FullName == name);
            type ??= AllTypes().FirstOrDefault(t => t.Name == name);

            if (type == null)
                CoreLibPlugin.Logger.LogWarning($"Could not find type named {name}");
            return type;
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

        #endregion

        #region PRIVATE

        private static bool _loaded;
        private static bool dumpCommandEnabled;

        [CoreLibSubmoduleInit(Stage = InitStage.GetOptionalDependencies)]
        internal static Type[] GetOptionalDeps()
        {
            dumpCommandEnabled = CoreLibPlugin.Instance.Config.Bind("Debug", "EnableDumpCommand", false, "Enable to allow object info to be dumped at runtime.").Value;

            if (dumpCommandEnabled)
            {
                return new[] { typeof(CommandsModule) };
            }

            return Array.Empty<Type>();
        }

        [CoreLibSubmoduleInit(Stage = InitStage.Load)]
        internal static void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<TemplateBlock>();
            RegisterJsonReaders(Assembly.GetExecutingAssembly());

            options = new JsonSerializerOptions
            {
                IncludeFields = true,
                WriteIndented = true
            };
            options.Converters.Add(new ObjectTypeConverter());
            options.Converters.Add(new ObjectIDConverter());
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new Il2CppListConverter());
            options.Converters.Add(new Il2CppStringConverter());
            options.Converters.Add(new SpriteConverter());
            options.Converters.Add(new ColorConverter());
            options.Converters.Add(new VectorConverter());
            options.Converters.Add(new RectConverter());
            
            // dummy converters
            options.Converters.Add(new IntPtrConverter());
            options.Converters.Add(new EntityMonoBehaviorDataConverter());
            options.Converters.Add(new GameObjectConverter());
            options.Converters.Add(new TransformConverter());
            interactionHandlers.Add(null);
        }

        [CoreLibSubmoduleInit(Stage = InitStage.PostLoad)]
        internal static void PostLoad()
        {
            ComponentModule.RegisterECSComponent<TemplateBlockCD>();
            ComponentModule.RegisterECSComponent<TemplateBlockCDAuthoring>();

            if (dumpCommandEnabled)
            {
                CommandsModule.RegisterCommandHandler(typeof(DumpCommandHandler), CoreLibPlugin.NAME);
            }
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
        public static JsonContext context;

        internal static Dictionary<string, IJsonReader> jsonReaders = new Dictionary<string, IJsonReader>();
        internal static Dictionary<string, string> modFolders = new Dictionary<string, string>();
        internal static List<object> interactionHandlers = new List<object>(10);

        internal static T GetInteractionHandler<T>(int index)
            where T : class
        {
            if (index <= 0)
            {
                throw new InvalidOperationException("Interaction handler is not valid!");
            }

            return interactionHandlers[index] as T;
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
                var attribute = fieldInfo.GetCustomAttribute<NonSerializedAttribute>();
                if (attribute != null) return;

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

        #endregion
    }
}