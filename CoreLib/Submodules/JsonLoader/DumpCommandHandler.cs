using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CoreLib.Submodules.ChatCommands;
using CoreLib.Submodules.JsonLoader.Converters;
using CoreLib.Util.Extensions;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using Unity.Mathematics;
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

        public CommandOutput Execute(string[] parameters)
        {
            if (parameters.Length < 1)
                return new CommandOutput("Not enough arguments. Please see usage!", Color.red);
            
            string item = parameters.Join(null, " ");
            string path = Path.Combine(Application.dataPath, "../dumps");

            CommandOutput output = CommandUtil.ParseItemName(item, out ObjectID objectID);
            if (objectID == ObjectID.None)
                return output;

            Directory.CreateDirectory(path);

            PugDatabaseAuthoring pugDB = Manager.ecs.pugDatabase;
            EntityMonoBehaviourData targetEntity = null;

            foreach (EntityMonoBehaviourData entity in pugDB.prefabList)
            {
                if (entity.objectInfo.objectID != objectID) continue;

                targetEntity = entity;
                break;
            }

            if (targetEntity == null)
                return new CommandOutput($"Failed to find entity for object ID {objectID}!", Color.red);

            try
            {
                PerformDump(path, targetEntity);
            }
            catch (Exception e)
            {
                CoreLibPlugin.Logger.LogWarning($"Failed to dump object {objectID}:\n{e}");
                return new CommandOutput("Failed to dump object! See log/console for error.", Color.red);
            }

            return "Dumped successfully!";
        }

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

            Il2CppArrayBase<Component> components = targetEntity.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component.GetIl2CppType() == Il2CppType.Of<Transform>()) continue;
                if (component.GetIl2CppType() == Il2CppType.Of<EntityMonoBehaviourData>()) continue;

                object actualComponent = component.CastToActualType();
                JsonNode componentNode = JsonSerializer.SerializeToNode(actualComponent, JsonLoaderModule.options);
                RemoveBadProperties(componentNode);
                componentNode["type"] = actualComponent.GetType().FullName;
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