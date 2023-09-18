using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreLib.Submodules.ChatCommands;
using CoreLib.Submodules.ChatCommands.Communication;
using CoreLib.Submodules.JsonLoader.Converters;
using CoreLib.Util.Extensions;
using HarmonyLib;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Submodules.JsonLoader
{
    public class DumpCommandHandler : IChatCommandHandler
    {
        private string[] badPropertyNames =
        {
            "ObjectClass",
            "Pointer",
            "WasCollected",
            "useGUILayout",
            "enabled",
            "isActiveAndEnabled",
            "transform",
            "gameObject",
            "tag",
            "m_CachedPtr",
            "name",
            "hideFlags",
            "entityMono"
        };

        private Type[] componentsToIgnore =
        {
            typeof(Transform),
            typeof(EntityMonoBehaviourData),
            typeof(ObjectAuthoring)
        };

        private static GameObject tmpGameObject;

        public class ObjectDumpData
        {
            [JsonPropertyName("$schema")]
            public string schema;
            
            public string type;
            public string itemId;
            
            public string localizedName;
            public string localizedDescription;
            
            // ObjectAuthoring properties
            public int initialAmount = 1;
            public int variation;
            public bool variationIsDynamic;
            public int variationToToggleTo;
            public ObjectType objectType;
            public List<ObjectCategoryTag> tags;
            public Rarity rarity;
            public bool isCustomScenePrefab;
            public List<Sprite> additionalSprites;

            public List<ComponentDumpData> components;
        }
        
        public class ComponentDumpData
        {
            public string type;
            [JsonPropertyName("$data")]
            public object data;
        }
        
        public CommandOutput Execute(string[] parameters, Entity conn)
        {
            if (parameters.Length < 1)
                return new CommandOutput("Not enough arguments. Please see usage!", CommandStatus.Error);
            
            string item = parameters.Join(null, " ");
            string path = Path.Combine(Application.dataPath, "../dumps");

            CommandOutput output = CommandUtil.ParseItemName(item, out ObjectID targetObjectId);
            if (targetObjectId == ObjectID.None)
                return output;

            Directory.CreateDirectory(path);

            PugDatabaseAuthoring pugDB = Manager.ecs.pugDatabase;
            MonoBehaviour targetEntity = null;

            foreach (MonoBehaviour entity in pugDB.prefabList)
            {
                var objectId = entity.GetEntityObjectID();
                if (objectId != targetObjectId) continue;

                targetEntity = entity;
                break;
            }

            if (targetEntity == null)
                return new CommandOutput($"Failed to find entity for object ID {targetObjectId}!", CommandStatus.Error);

            tmpGameObject = new GameObject();
            try
            {
               PerformDump(path, targetEntity);
            }
            catch (Exception e)
            {
                CoreLibMod.Log.LogWarning($"Failed to dump object {targetObjectId}:\n{e}");
                Object.Destroy(tmpGameObject);
                return new CommandOutput("Failed to dump object! See log/console for error.", CommandStatus.Error);
            }
            Object.Destroy(tmpGameObject);
            return "Dumped successfully!";
        }

        private void PerformDump(string path, MonoBehaviour targetEntity)
        {
            SpriteConverter.outputPath = Path.Combine(path, "icons");
            Directory.CreateDirectory(SpriteConverter.outputPath);

            IEntityMonoBehaviourData entityData = targetEntity.GetComponent<IEntityMonoBehaviourData>();
            var objectInfo = entityData.ObjectInfo;
            
            ObjectDumpData dumpData = new ObjectDumpData();

            //JsonNode node = JsonSerializer.SerializeToNode(targetEntity.objectInfo, JsonLoaderModule.options);
            //JsonObject jsonObject = node.AsObject();
            string kind = GetObjectType(objectInfo.objectType);

            dumpData.schema = "https://raw.githubusercontent.com/Jrprogrammer/CoreLib/master/CoreLib/Submodules/JsonLoader/Schemas/entity_schema.json";
            dumpData.type = GetObjectType(objectInfo.objectType);
            
            dumpData.itemId = objectInfo.objectID.ToString();

            var components = targetEntity.GetComponents<Component>();
            List<ComponentDumpData> componentDumpDatas = new List<ComponentDumpData>(components.Length);
            
            foreach (Component component in components)
            {
                if (componentsToIgnore.Contains(component.GetType())) continue;
                ComponentDumpData componentData = new ComponentDumpData
                {
                    type = component.GetType().FullName,
                    data = component
                };
                componentDumpDatas.Add(componentData);
            }

            dumpData.components = componentDumpDatas;
            
            CopyObjectData(targetEntity, dumpData);

            var jsonData = JsonSerializer.Serialize(dumpData, JsonLoaderModule.options);

            SpriteConverter.outputPath = "";

            string outFolder = Path.Combine(path, kind);
            Directory.CreateDirectory(outFolder);
            string filePath = Path.Combine(outFolder, $"{objectInfo.objectID}.json");
            File.WriteAllText(filePath, jsonData);
        }

        private static void CopyObjectData(MonoBehaviour entity, ObjectDumpData dumpData)
        {
            if (entity is EntityMonoBehaviourData entityMonoBehaviourData)
            {
                CopyEntityMonoBehaviourData(entityMonoBehaviourData, dumpData);
            }else if (entity is ObjectAuthoring objectAuthoring)
            {
                CopyObjectAuthoringData(objectAuthoring, dumpData);
            }
        }
        
        private static void CopyObjectAuthoringData(ObjectAuthoring objectAuthoring, ObjectDumpData dumpData)
        {
            dumpData.initialAmount = objectAuthoring.initialAmount;
            dumpData.variation = objectAuthoring.variation;
            dumpData.variationIsDynamic = objectAuthoring.variationIsDynamic;
            dumpData.variationToToggleTo = objectAuthoring.variationToToggleTo;
            dumpData.objectType = objectAuthoring.objectType;
            dumpData.tags = objectAuthoring.tags;
            dumpData.rarity = objectAuthoring.rarity;
            dumpData.isCustomScenePrefab = objectAuthoring.isCustomScenePrefab;
            dumpData.additionalSprites = objectAuthoring.additionalSprites;
        }
        
        private static void CopyEntityMonoBehaviourData(EntityMonoBehaviourData entityData, ObjectDumpData dumpData )
        {
            dumpData.initialAmount = entityData.ObjectInfo.initialAmount;
            dumpData.variation = entityData.ObjectInfo.variation;
            dumpData.variationIsDynamic = entityData.ObjectInfo.variationIsDynamic;
            dumpData.variationToToggleTo = entityData.ObjectInfo.variationToToggleTo;
            dumpData.objectType = entityData.ObjectInfo.objectType;
            dumpData.tags = entityData.ObjectInfo.tags;
            dumpData.rarity = entityData.ObjectInfo.rarity;
            dumpData.isCustomScenePrefab = entityData.ObjectInfo.isCustomScenePrefab;
            dumpData.additionalSprites = entityData.ObjectInfo.additionalSprites;

            var itemAuthoring = entityData.GetComponent<InventoryItemAuthoring>();

            if (itemAuthoring == null)
            {
                itemAuthoring = tmpGameObject.AddComponent<InventoryItemAuthoring>();
                dumpData.components.Add(new ComponentDumpData()
                {
                    type = typeof(InventoryItemAuthoring).FullName,
                    data = itemAuthoring
                });

                itemAuthoring.onlyExistsInSeason = (int)entityData.ObjectInfo.onlyExistsInSeason;
                itemAuthoring.sellValue = entityData.ObjectInfo.sellValue;
                itemAuthoring.buyValueMultiplier = entityData.ObjectInfo.buyValueMultiplier;
                itemAuthoring.icon = entityData.ObjectInfo.icon;
                itemAuthoring.iconOffset = entityData.ObjectInfo.iconOffset;
                itemAuthoring.smallIcon = entityData.ObjectInfo.smallIcon;
                itemAuthoring.isStackable = entityData.ObjectInfo.isStackable;
                itemAuthoring.craftingSettings = entityData.ObjectInfo.craftingSettings;
                itemAuthoring.requiredObjectsToCraft = entityData.ObjectInfo.requiredObjectsToCraft
                    .Select(o => new InventoryItemAuthoring.CraftingObject()
                    {
                        objectID = (int)o.objectID,
                        amount = o.amount
                    }).ToList();
                itemAuthoring.craftingTime = entityData.ObjectInfo.craftingTime;
            }
            
            var placeableObject = entityData.GetComponent<PlaceableObjectAuthoring>();
            if (placeableObject == null)
            {
                placeableObject = tmpGameObject.AddComponent<PlaceableObjectAuthoring>();
                dumpData.components.Add(new ComponentDumpData()
                {
                    type = typeof(PlaceableObjectAuthoring).FullName,
                    data = placeableObject
                });
            }

            placeableObject.prefabTileSize = entityData.ObjectInfo.prefabTileSize;
            placeableObject.prefabCornerOffset = entityData.ObjectInfo.prefabCornerOffset;
        }

        private string GetObjectType(ObjectType objectType)
        {
            switch (objectType)
            {
                case ObjectType.PlaceablePrefab:
                case ObjectType.Critter:
                case ObjectType.Creature:
                case ObjectType.PlayerType:
                case ObjectType.NonObtainable:
                    return "block";

                default:
                    return "item";
            }
        }
        
        public string GetDescription()
        {
            return "Dump any object entity in JSON format for inspection and recreation\n" +
                   "/dump {object} {output path}";
        }

        public string[] GetTriggerNames()
        {
            return new[] { "dump" };
        }
    }
}