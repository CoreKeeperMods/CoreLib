using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using CoreLib.Components;
using CoreLib.Submodules.CustomEntity;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    [RegisterReader("item")]
    public class ItemJsonReader : IJsonReader
    {
        public virtual void ApplyPre(JsonNode jObject)
        {
            string itemId = jObject["itemId"].GetValue<string>();
            GameObject go = new GameObject();
            EntityMonoBehaviourData entityData = go.AddComponent<EntityMonoBehaviourData>();

            ReadObjectInfo(jObject, entityData);
            go.hideFlags = HideFlags.HideAndDontSave;

            ReadComponents(jObject, entityData);

            CustomEntityModule.CallAlloc(entityData);
            ObjectID objectID = CustomEntityModule.AddEntityWithVariations(itemId, new System.Collections.Generic.List<EntityMonoBehaviourData> { entityData });

            ReadLocalization(jObject, objectID);
        }
        
        public virtual void ApplyPost(JsonNode jObject)
        {
            string itemId = jObject["itemId"].GetValue<string>();
            ObjectID objectID = CustomEntityModule.GetObjectId(itemId);

            if (CustomEntityModule.GetMainEntity(objectID, out EntityMonoBehaviourData entity))
            {
                if (jObject["requiredObjectsToCraft"] != null)
                {
                    List<CraftingObject> recipe = jObject["requiredObjectsToCraft"].Deserialize<List<CraftingObject>>(JsonLoaderModule.options);
                    entity.objectInfo.requiredObjectsToCraft = recipe;
                }
                return;
            }

            throw new InvalidOperationException($"Failed to find item with ID {itemId}!");
        }

        public static void ReadComponents(JsonNode jObject, EntityMonoBehaviourData entityData)
        {
            if (jObject["components"] != null)
            {
                JsonArray array = jObject["components"].AsArray();
                foreach (JsonNode node in array)
                {
                    if (node["type"] != null)
                    {
                        Type sysType = JsonLoaderModule.TypeByName(node["type"].GetValue<string>());
                        Il2CppSystem.Type type = Il2CppType.From(sysType);
                        
                        Component component = entityData.gameObject.GetComponent(type);
                        if (component == null)
                            component = entityData.gameObject.AddComponent(type);
                        
                        MethodInfo methonGen = typeof(Il2CppObjectBase).GetMethod(nameof(Il2CppObjectBase.Cast), AccessTools.all);
                        MethodInfo method = methonGen.MakeGenericMethod(sysType);
                        object castComponent = method.Invoke(component, Array.Empty<object>());
                        JsonLoaderModule.PopulateObject(sysType, castComponent, node);
                        JsonLoaderModule.FillArrays(sysType, castComponent);

                        MonoBehaviour componentMono = component.TryCast<MonoBehaviour>();
                        if (componentMono != null)
                            CustomEntityModule.CallAlloc(componentMono);
                    }
                }
            }
        }

        public static void ReadObjectInfo(JsonNode jObject, EntityMonoBehaviourData entityData)
        {
            string itemId = jObject["itemId"].GetValue<string>();
            
            entityData.objectInfo ??= new ObjectInfo();
            JsonLoaderModule.PopulateObject(entityData.objectInfo, jObject);
            //entityData.objectInfo = jObject.Deserialize<ObjectInfo>(JsonLoaderModule.options);

            if (entityData.objectInfo.prefabInfos == null)
            {
                entityData.objectInfo.prefabInfos = new List<PrefabInfo>();
                entityData.objectInfo.prefabInfos.Add(
                    new PrefabInfo()
                    {
                        ecsPrefab = entityData.gameObject
                    });
            }

            string fullItemId = $"{itemId}_{entityData.objectInfo.variation}";
            
            entityData.gameObject.name = $"{fullItemId}_Prefab";
            JsonLoaderModule.FillArrays(entityData.objectInfo);
        }

        public static void ReadLocalization(JsonNode jObject, ObjectID objectID)
        {
            if (jObject["localizedName"] != null && jObject["localizedDescription"] != null)
            {
                string localizedName = jObject["localizedName"].GetValue<string>();
                string localizedDescription = jObject["localizedDescription"].GetValue<string>();

                CustomEntityModule.AddEntityLocalization(objectID, localizedName, localizedDescription);
            }
        }
    }
}