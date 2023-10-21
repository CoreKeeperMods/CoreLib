using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreLib.Submodules.ModEntity;
using CoreLib.Util.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreLib.Drops;
using CoreLib.JsonLoader;
using CoreLib.JsonLoader.Converters;
using CoreLib.JsonLoader.Patch;
using CoreLib.JsonLoader.Readers;
using CoreLib.Localization;
using CoreLib.ModResources;
using JetBrains.Annotations;
using PugMod;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;
using MemberInfo = PugMod.MemberInfo;

[assembly:GenerateVectorReaders]

namespace CoreLib.JsonLoader
{
    public class JsonLoaderModule : BaseSubmodule
    {
        #region PUBLIC_INTERFACE
        
        public static IFileAccess fileAccess = new NoFileAccess();

        internal override GameVersion Build => new GameVersion(0, 7, 0, 3, "25d3");
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
            string path = API.ModLoader.GetDirectory(modInfo.ModId);
            var modGuid = modInfo.Metadata.name;
            
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate();

            if (modFolders.ContainsKey(modGuid))
            {
                CoreLibMod.Log.LogWarning($"Trying to load mod {modGuid} folder again!");
                return;
            }

            using (WithContext(new JsonContext(modInfo, "JsonResources")))
            {
                var modJsonFiles = modInfo.Metadata.files
                    .Where(file => file.path.EndsWith(".json") && file.path.Contains("JsonResources"))
                    .Select(file => file.path);
                
                foreach (var file in modJsonFiles)
                {
                    string filename = file.GetFileName();
                    var jsonText = modInfo.GetAllText(file);
                    JsonDocument document = JsonDocument.Parse(jsonText);
                    JsonElement jObject = document.RootElement;
                    var fileReference = new FileReference(modInfo, file, "JsonResources");

                    if (!jObject.TryGetProperty("type", out var typeElement))
                    {
                        CoreLibMod.Log.LogWarning(
                            $"JSON definition file {file} does not contain type information! Please specify 'type' value!");
                        continue;
                    }

                    string type = typeElement.GetString();

                    if (jsonReaders.ContainsKey(type))
                    {
                        try
                        {
                            CoreLibMod.Log.LogInfo($"Loading JSON file {filename} with {type} reader.");
                            IJsonReader reader = jsonReaders[type];
                            reader.ApplyPre(jObject, fileReference);
                            postApplyFiles.Add(fileReference);
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
        
        public static void RegisterJsonReaders(long modId)
        {
            IEnumerable<Type> types = API.Reflection.GetTypes(modId)
                .Where(type => type.GetAttributeChecked<RegisterReaderAttribute>() != null);

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

            Type[] allTypes = API.Reflection.AllTypes();
            
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

        private static bool finishedLoadingObjects = false;
        private static bool entityModificationFileCacheReady = false;

        public static JsonSerializerOptions options;
        public static JsonContext context;

        internal static Dictionary<string, IJsonReader> jsonReaders = new Dictionary<string, IJsonReader>();
        internal static Dictionary<string, string> modFolders = new Dictionary<string, string>();
        internal static List<IInteractionHandler> interactionHandlers = new List<IInteractionHandler>();
        internal static List<FileReference> entityModificationFiles = new List<FileReference>();

        private static List<FileReference> postApplyFiles = new List<FileReference>();
        private static Dictionary<ObjectID, FileReference> entityModificationFileCache = new Dictionary<ObjectID, FileReference>();
        
        #region Init

        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(MemoryManager_Patch_2));
        }

        internal override void Load()
        {
            ResourcesModule.RefreshModuleBundles();
            RegisterJsonReaders(CoreLibMod.modInfo.ModId);

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
            foreach (var file in postApplyFiles)
            {
                string filename = file.filePath.GetFileName();
                var jsonText = file.mod.GetAllText(file.filePath);
                var jObject = JsonDocument.Parse(jsonText).RootElement;

                string type = jObject.GetProperty("type").GetString();

                if (jsonReaders.ContainsKey(type))
                {
                    try
                    {
                        CoreLibMod.Log.LogInfo($"Post Loading JSON file {filename} with {type} reader.");
                        IJsonReader reader = jsonReaders[type];
                        reader.ApplyPost(jObject, file);
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
                FileReference modify = entityModificationFileCache[objectId];
                var jsonText = modify.mod.GetAllText(modify.filePath);
                JsonDocument jObject = JsonDocument.Parse(jsonText);

                using (WithContext(new JsonContext(modify.mod, modify.contextPath)))
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
                FileReference modify = entityModificationFileCache[objectId];
                var jsonText = modify.mod.GetAllText(modify.filePath);
                JsonDocument jObject = JsonDocument.Parse(jsonText);

                using (WithContext(new JsonContext(modify.mod, modify.contextPath)))
                {
                    ModificationJsonReader.ModifyPost(jObject.RootElement, entity, authoring, entityManager);
                }

                jObject.Dispose();
            }
        }

        private static void BuildModificationCache()
        {
            if (entityModificationFileCacheReady) return;

            foreach (FileReference modifyFile in entityModificationFiles)
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
            RegisterReaderAttribute attribute = type.GetAttributeChecked<RegisterReaderAttribute>();
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

            var attribute = ((MemberInfo)fieldInfo).HasAttributeChecked<NonSerializedAttribute>();
            if (attribute) return;

            var parsedValue = property.Value.Deserialize((fieldInfo).FieldType, options);
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
                        var oldNameAttr = ((MemberInfo)field).GetAttributeChecked<FormerlySerializedAsAttribute>();
                        return oldNameAttr != null && oldNameAttr.oldName.Equals(name);
                    }).FirstOrDefault();
            }

            return fieldInfo;
        }

        #endregion
    }
}