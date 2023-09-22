using System;
using System.Text.Json;
using CoreLib.Util.Extensions;
using PugMod;
using Unity.Entities;
using UnityEngine;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    [RegisterReader("modify")]
    public class ModificationJsonReader : IJsonReader
    {
        public void ApplyPre(JsonElement jObject, FileReference context)
        {
            string targetId = jObject.GetProperty("targetId").GetString();
            context.targetId = targetId;
            JsonLoaderModule.entityModificationFiles.Add(context);
        }

        public static void ModifyPre(JsonElement jObject, MonoBehaviour entity)
        {
            var entityMonoBehaviorData = entity.GetComponent<EntityMonoBehaviourData>();
            var objectAuthoring = entity.GetComponent<ObjectAuthoring>();
            var inventoryItemAuthoring = entity.GetComponent<InventoryItemAuthoring>();

            if (entityMonoBehaviorData != null)
                JsonLoaderModule.PopulateObject(entityMonoBehaviorData.objectInfo, jObject, ItemJsonReader.restrictedProperties);

            if (objectAuthoring != null)
                JsonLoaderModule.PopulateObject(objectAuthoring, jObject, ItemJsonReader.restrictedProperties);

            if (inventoryItemAuthoring != null)
                JsonLoaderModule.PopulateObject(inventoryItemAuthoring, jObject, ItemJsonReader.restrictedProperties);

            ItemJsonReader.ReadComponents(jObject, entity.gameObject);
        }

        public static void ModifyPost(JsonElement jObject, Entity entity, GameObject authoring, EntityManager entityManager)
        {
            ReadECSComponents(jObject, entity, entityManager);
        }

        private static void ReadECSComponents(JsonElement jObject, Entity entity, EntityManager entityManager)
        {
            if (jObject.TryGetProperty("ecsComponents", out var componentsElement))
            {
                foreach (var node in componentsElement.EnumerateArray())
                {
                    if (node.TryGetProperty("type", out var typeElement))
                    {
                        Type type = JsonLoaderModule.TypeByName(typeElement.GetString());
                        if (!type.IsValueType)
                        {
                            CoreLibMod.Log.LogWarning($"Failed to edit component '{type.FullName}': Non Value type components are not supported!");
                            continue;
                        }

                        if (type.IsAssignableTo(typeof(IComponentData)))
                        {
                            API.Reflection.Invoke(typeof(ModificationJsonReader)
                                .GetMethod(nameof(ReadComponent))
                                .MakeGenericMethod(type), null, 
                                jObject, entity, entityManager);
                        }else if (type.IsAssignableTo(typeof(IBufferElementData)))
                        {
                            API.Reflection.Invoke(typeof(ModificationJsonReader)
                                .GetMethod(nameof(ReadBuffer))
                                .MakeGenericMethod(type), null, 
                                jObject, entity, entityManager);
                        }
                        else
                        {
                            CoreLibMod.Log.LogWarning($"Failed to edit component '{type.FullName}': Unknown component kind!");
                        }
                    }
                }
            }
        }

        private static void ReadComponent<T>(JsonElement jObject, Entity entity, EntityManager entityManager)
        where T : struct, IComponentData
        {
            T component = entityManager.GetOrAddComponentData<T>(entity);

            object boxedComponent = component;
            JsonLoaderModule.PopulateObject(typeof(T), boxedComponent, jObject);
            component = (T)boxedComponent;
            
            entityManager.SetComponentData(entity, component);
        }

        private static void ReadBuffer<T>(JsonElement jObject, Entity entity, EntityManager entityManager)
            where T : struct, IBufferElementData
        {
            var buffer = entityManager.GetOrAddBuffer<T>(entity);

            ModifyMode mode = ModifyMode.Edit;
            if (jObject.TryGetProperty("modifyMode", out var modeElement))
            {
                mode = modeElement.Deserialize<ModifyMode>(JsonLoaderModule.options);
            }

            if (mode == ModifyMode.Edit)
            {
                if (jObject.TryGetProperty("remove", out var removeElement))
                {
                    var elements = removeElement.Deserialize<T[]>(JsonLoaderModule.options);
                    foreach (T bufferElementData in elements)
                    {
                        buffer.Remove(bufferElementData);
                    }
                }
            }else if (mode == ModifyMode.Overwrite)
            {
                buffer.Clear();
            }
            
            if (jObject.TryGetProperty("add", out var addElement))
            {
                var elements = addElement.Deserialize<T[]>(JsonLoaderModule.options);
                foreach (T bufferElementData in elements)
                {
                    buffer.Add(bufferElementData);
                }
            }
        }

        public enum ModifyMode
        {
            Edit,
            Overwrite
        }

        public void ApplyPost(JsonElement jObject, FileReference context) { }
    }
}