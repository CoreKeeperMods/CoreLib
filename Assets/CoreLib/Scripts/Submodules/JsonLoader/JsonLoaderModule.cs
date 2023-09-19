using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CoreLib.Submodules.ChatCommands;
using CoreLib.Submodules.DropTables;
using CoreLib.Submodules.ModEntity;
using CoreLib.Submodules.JsonLoader.Converters;
using CoreLib.Submodules.JsonLoader.Readers;
using CoreLib.Util.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreLib.Submodules.JsonLoader.Patch;
using CoreLib.Submodules.Localization;
using JetBrains.Annotations;
using PugMod;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace CoreLib.Submodules.JsonLoader
{
    public class JsonLoaderModule : BaseSubmodule
    {
        #region PUBLIC_INTERFACE

        internal override GameVersion Build => new GameVersion(0, 0, 0, 0, "");
        internal override Type[] Dependencies => new[] { typeof(EntityModule), typeof(DropTablesModule), typeof(LocalizationModule) };
        internal static JsonLoaderModule Instance => CoreLibMod.GetModuleInstance<JsonLoaderModule>();

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

        [UsedImplicitly]
        public static void LoadMod(IMod mod)
        {
            var modInfo = mod.GetModInfo();
            string directory = API.ModLoader.GetDirectory(modInfo.ModId);
            LoadFolder(modInfo.Metadata.name, directory);
        }

        public static void LoadFolder(string modGuid, string path)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate();

            if (modFolders.ContainsKey(modGuid))
            {
                CoreLibMod.Log.LogWarning($"Trying to load mod {modGuid} folder again!");
                return;
            }

            if (!Directory.Exists(Path.Combine(path, "JsonResources")))
            {
                CoreLibMod.Log.LogWarning($"Mod {modGuid} folder does not contain 'JsonResources' folder!");
                return;
            }

            string resourcesDir = Path.Combine(path, "JsonResources");

            using (WithContext(new JsonContext(resourcesDir)))
            {
                foreach (string file in Directory.EnumerateFiles(resourcesDir, "*.json", SearchOption.AllDirectories))
                {
                    string filename = file.Substring(file.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                    JsonDocument document = JsonDocument.Parse(File.ReadAllText(file));
                    JsonElement jObject = document.RootElement;
                    FileContext fileContext = new FileContext(file, filename);

                    if (!jObject.TryGetProperty("type", out var typeElement))
                    {
                        CoreLibMod.Log.LogWarning(
                            $"JSON definition file {resourcesDir.GetRelativePath(file)} does not contain type information! Please specify 'type' value!");
                        continue;
                    }

                    string type = typeElement.GetString();

                    if (jsonReaders.ContainsKey(type))
                    {
                        try
                        {
                            CoreLibMod.Log.LogInfo($"Loading JSON file {filename} with {type} reader.");
                            IJsonReader reader = jsonReaders[type];
                            reader.ApplyPre(jObject, fileContext);
                            postApplyFiles.Add(file);
                        }
                        catch (Exception e)
                        {
                            CoreLibMod.Log.LogError($"Failed to add object:\n{e}");
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

        public static void RegisterInteractHandler<T>()
            where T : IInteractionHandler, new()
        {
            int existingMethod = interactionHandlers.FindIndex(info => info != null && info.GetType() == typeof(T));
            if (existingMethod > 0) return;
            
            CoreLibMod.Log.LogDebug($"Registering {typeof(T)} as object interact handler!");
            interactionHandlers.Add(new T());
        }

        public static Type TypeByName(string name)
        {
            Type type = Type.GetType(name, false);

            Type[] allTypes = ReflectionUtil.AllTypes();
            
            type ??= allTypes.FirstOrDefault(t => t.FullName == name);
            type ??= allTypes.FirstOrDefault(t => t.Name == name);

            if (type == null)
                CoreLibMod.Log.LogWarning($"Could not find type named {name}");
            return type;
        }

        public static void FillArrays<T>(T target)
        {
            if (target == null) return;
            
            FillArrays(typeof(T), target);
        }

        public static void FillArrays(Type type, object target)
        {
            foreach (FieldInfo property in type.GetFields())
            {
                if (!property.FieldType.IsGenericType) continue;

                if (property.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    object value = property.GetValue(target);
                    if (value == null)
                    {
                        property.SetValue(target, Activator.CreateInstance(property.FieldType));
                    }
                }
            }
        }

        public static void PopulateObject<T>(T target, JsonElement jsonSource)
        {
            PopulateObject(typeof(T), target, jsonSource, Array.Empty<string>(), Array.Empty<Type>());
        }

        public static void PopulateObject<T>(T target, JsonElement jsonSource, string[] restricted)
        {
            PopulateObject(typeof(T), target, jsonSource, restricted, Array.Empty<Type>());
        }

        public static void PopulateObject<T>(T target, JsonElement jsonSource, string[] restricted, Type[] typeSet)
        {
            PopulateObject(typeof(T), target, jsonSource, restricted, typeSet);
        }

        public static void PopulateObject(Type type, object target, JsonElement jsonSource)
        {
            PopulateObject(type, target, jsonSource, Array.Empty<string>(), Array.Empty<Type>());
        }

        public static void PopulateObject(Type type, object target, JsonElement jsonSource, string[] restricted, Type[] typeSet)
        {
            if (target == null) return;
            
            foreach (JsonProperty property in jsonSource.EnumerateObject())
            {
                if (restricted.Contains(property.Name))
                {
                    CoreLibMod.Log.LogWarning($"Overriding {property.Name} is not allowed!");
                    continue;
                }

                try
                {
                    OverwriteField(type, target, property, typeSet);
                }
                catch (Exception e)
                {
                    CoreLibMod.Log.LogWarning($"Failed to deserialize field/property '{property.Name}':\n{e}");
                }
            }
        }

        #endregion

        #region PRIVATE

        private static readonly string[] specialProperties =
        {
            "$schema",
            "type",
            "itemId",
            "requiredObjectsToCraft",
            "components",
            "localizedName",
            "localizedDescription",
            "colliderSize",
            "colliderCenter"
        };

        private static bool dumpCommandEnabled;
        private static bool finishedLoadingObjects = false;
        private static bool entityModificationFileCacheReady = false;

        public static JsonSerializerOptions options;
        public static JsonContext context;

        internal static Dictionary<string, IJsonReader> jsonReaders = new Dictionary<string, IJsonReader>();
        internal static Dictionary<string, string> modFolders = new Dictionary<string, string>();
        internal static List<IInteractionHandler> interactionHandlers = new List<IInteractionHandler>();
        internal static List<ModifyFile> entityModificationFiles = new List<ModifyFile>();

        private static List<string> postApplyFiles = new List<string>();
        private static Dictionary<ObjectID, ModifyFile> entityModificationFileCache = new Dictionary<ObjectID, ModifyFile>();
        
        

        #region Init

        internal override Type[] GetOptionalDependencies()
        {
            //dumpCommandEnabled = CoreLibMod.Config.Bind("Debug", "EnableDumpCommand", false, "Enable to allow object info to be dumped at runtime.").Value;

            if (dumpCommandEnabled)
            {
                return new[] { typeof(CommandsModule) };
            }

            return Array.Empty<Type>();
        }

        internal override void SetHooks()
        {
            CoreLibMod.harmony.PatchAll(typeof(MemoryManager_Patch_2));
        }

        internal override void Load()
        {
            RegisterJsonReaders(Assembly.GetExecutingAssembly());

            options = new JsonSerializerOptions
            {
                IncludeFields = true,
                IgnoreReadOnlyProperties = true,
                WriteIndented = true,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };
            options.Converters.Add(new ObjectTypeConverter());
            options.Converters.Add(new ObjectIDConverter());
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new SpriteConverter());
            options.Converters.Add(new ColorConverter());
            options.AddGeneratedVectorConverters();

            options.Converters.Add(new RectConverter());
            options.Converters.Add(new Texture2DConverter());
            options.Converters.Add(new LootTableIDConverter());
            options.Converters.Add(new CraftableObjectConverter());
            options.Converters.Add(new CraftingObjectConverter());

            // dummy converters
            options.Converters.Add(new IntPtrConverter());
            options.Converters.Add(new EntityMonoBehaviorDataConverter());
            options.Converters.Add(new GameObjectConverter());
            options.Converters.Add(new TransformConverter());
            options.Converters.Add(new EntityMonoBehaviorConverter());
            interactionHandlers.Add(null);

            API.Authoring.OnObjectTypeAdded += PostConversionModificationsApply;
        }

        internal override void PostLoad()
        {
            if (dumpCommandEnabled)
            {
                CommandsModule.RegisterCommandHandler(typeof(DumpCommandHandler), CoreLibMod.NAME);
            }
        }

        internal static void ThrowIfTooLate()
        {
            if (finishedLoadingObjects)
            {
                throw new InvalidOperationException("Json Loader finished loading items. Adding items at this stage is impossible!");
            }
        }

        #endregion

        #region Modification

        internal static void PostApply()
        {
            CoreLibMod.Log.LogInfo("Start pre conversion modification");
            foreach (MonoBehaviour entity in Manager.ecs.pugDatabase.prefabList)
            {
                PreConversionModificationsApply(entity);
            }

            CoreLibMod.Log.LogInfo("Start JSON post load");
            foreach (string file in postApplyFiles)
            {
                string filename = file.Substring(file.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                var jObject = JsonDocument.Parse(File.ReadAllText(file)).RootElement;
                FileContext fileContext = new FileContext(file, filename);

                string type = jObject.GetProperty("type").GetString();

                if (jsonReaders.ContainsKey(type))
                {
                    try
                    {
                        CoreLibMod.Log.LogInfo($"Post Loading JSON file {filename} with {type} reader.");
                        IJsonReader reader = jsonReaders[type];
                        reader.ApplyPost(jObject, fileContext);
                    }
                    catch (Exception e)
                    {
                        CoreLibMod.Log.LogError($"Failed to post load:\n{e}");
                    }
                }
            }

            postApplyFiles.Clear();
        }

        internal static void PreConversionModificationsApply(MonoBehaviour entity)
        {
            BuildModificationCache();

            var objectId = entity.gameObject.GetEntityObjectID();

            if (entityModificationFileCache.ContainsKey(objectId))
            {
                ModifyFile modify = entityModificationFileCache[objectId];
                JsonDocument jObject = JsonDocument.Parse(File.ReadAllText(modify.filePath));

                using (WithContext(new JsonContext(modify.contextPath)))
                {
                    ModificationJsonReader.ModifyPre(jObject.RootElement, entity);
                }

                jObject.Dispose();
            }
        }

        internal static void PostConversionModificationsApply(Entity entity, GameObject authoring, EntityManager entityManager)
        {
            var objectId = authoring.GetEntityObjectID();

            if (entityModificationFileCache.ContainsKey(objectId))
            {
                ModifyFile modify = entityModificationFileCache[objectId];
                JsonDocument jObject = JsonDocument.Parse(File.ReadAllText(modify.filePath));

                using (WithContext(new JsonContext(modify.contextPath)))
                {
                    ModificationJsonReader.ModifyPost(jObject.RootElement, entity, authoring, entityManager);
                }

                jObject.Dispose();
            }
        }

        private static void BuildModificationCache()
        {
            if (entityModificationFileCacheReady) return;

            foreach (ModifyFile modifyFile in entityModificationFiles)
            {
                ObjectID objectID = modifyFile.targetId.GetObjectID();
                if (objectID == ObjectID.None)
                {
                    CoreLibMod.Log.LogError($"Failed to apply entity modification, '{modifyFile.targetId}' is not a valid entity!");
                    continue;
                }

                entityModificationFileCache.Add(objectID, modifyFile);
            }

            entityModificationFileCacheReady = true;
            entityModificationFiles.Clear();
        }

        #endregion

        internal static T GetInteractionHandler<T>(int index)
            where T : class
        {
            if (index <= 0) return null;

            return interactionHandlers[index] as T;
        }
        
        internal static int GetInteractHandlerId(string typeName)
        {
            int existingMethod = interactionHandlers.FindIndex(info =>
            {
                return info != null && 
                       info.GetType().FullName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase);
            });
            
            if (existingMethod > 0)
            {
                return existingMethod;
            }

            return 0;
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
                CoreLibMod.Log.LogError($"Failed to register {type.FullName} Json Reader, because name {attribute.typeName} is already taken!");
            }
        }

        private static void OverwriteField(Type type, object target, JsonProperty property, Type[] typeSet)
        {
            FieldInfo fieldInfo = FindFieldInType(type, property.Name);

            if (fieldInfo == null)
            {
                if (!specialProperties.Contains(property.Name) &&
                    typeSet.All(otherType => FindFieldInType(otherType, property.Name) == null))
                {
                    CoreLibMod.Log.LogWarning($"Property '{property.Name}' not found!");
                }

                return;
            }


            var attribute = fieldInfo.GetCustomAttribute<NonSerializedAttribute>();
            if (attribute != null) return;

            var parsedValue = property.Value.Deserialize(fieldInfo.FieldType, options);
            fieldInfo.SetValue(target, parsedValue);
        }

        private static FieldInfo FindFieldInType(Type type, string name)
        {
            var fieldInfo = type.GetField(name);

            if (fieldInfo == null)
            {
                fieldInfo = type.GetFields()
                    .Where(field =>
                    {
                        var oldNameAttr = field.GetCustomAttribute<FormerlySerializedAsAttribute>();
                        return oldNameAttr != null && oldNameAttr.oldName.Equals(name);
                    }).FirstOrDefault();
            }

            return fieldInfo;
        }

        #endregion
    }
}