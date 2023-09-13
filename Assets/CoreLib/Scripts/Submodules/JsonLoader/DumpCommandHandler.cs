using System;
using System.IO;
using CoreLib.Scripts.Util.Extensions;
using CoreLib.Submodules.ChatCommands;
using CoreLib.Submodules.ChatCommands.Communication;
using HarmonyLib;
using Unity.Entities;
using UnityEngine;

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

            try
            {
               // PerformDump(path, targetEntity);
            }
            catch (Exception e)
            {
                CoreLibMod.Log.LogWarning($"Failed to dump object {targetObjectId}:\n{e}");
                return new CommandOutput("Failed to dump object! See log/console for error.", CommandStatus.Error);
            }

            return "Feature disabled!";
        }
/*
        private void PerformDump(string path, EntityMonoBehaviourData targetEntity)
        {
            SpriteConverter.outputPath = Path.Combine(path, "icons");
            Directory.CreateDirectory(SpriteConverter.outputPath);

            JsonNode node = JsonSerializer.SerializeToNode(targetEntity.objectInfo, JsonLoaderModule.options);
            JsonObject jsonObject = node.AsObject();
            string kind = GetObjectType(targetEntity.objectInfo.objectType);

            node["$schema"] = "https://raw.githubusercontent.com/Jrprogrammer/CoreLib/master/CoreLib/Submodules/JsonLoader/Schemas/entity_schema.json";
            node["type"] = GetObjectType(targetEntity.objectInfo.objectType);
            ;
            node["itemId"] = targetEntity.objectInfo.objectID.ToString();

            RemoveBadProperties(node);
            JsonArray itemNeeded = node["requiredObjectsToCraft"].AsArray();
            foreach (JsonNode jsonNode in itemNeeded)
            {
                RemoveBadProperties(jsonNode);
            }
            
            jsonObject.Remove("objectID");
            jsonObject.Remove("prefabInfos");

            JsonArray array = new JsonArray();
            
            var components = targetEntity.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component.GetType() == typeof(Transform)) continue;
                if (component.GetType() == typeof(EntityMonoBehaviourData)) continue;

                JsonNode componentNode = JsonSerializer.SerializeToNode(component, JsonLoaderModule.options);
                RemoveBadProperties(componentNode);
                componentNode["type"] = component.GetType().FullName;
                array.Add(componentNode);
            }

            node["components"] = array;

            SpriteConverter.outputPath = "";

            string outFolder = Path.Combine(path, kind);
            Directory.CreateDirectory(outFolder);
            string filePath = Path.Combine(outFolder, $"{targetEntity.objectInfo.objectID}.json");
            File.WriteAllText(filePath, node.ToJsonString(JsonLoaderModule.options));
        }

        private void RemoveBadProperties(JsonNode node)
        {
            try
            {
                JsonObject jsonObject = node.AsObject();

                foreach (string propertyName in badPropertyNames)
                {
                    jsonObject.Remove(propertyName);
                }
            }
            catch (Exception)
            {
                //ignored
            }
        }
*/
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