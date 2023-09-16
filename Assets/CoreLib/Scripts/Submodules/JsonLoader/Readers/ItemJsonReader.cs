﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CoreLib.Components;
using CoreLib.Submodules.ModEntity;
using CoreLib.Submodules.Localization;
using CoreLib.Util.Extensions;
using UnityEngine;

namespace CoreLib.Submodules.JsonLoader.Readers
{
    [RegisterReader("item")]
    public class ItemJsonReader : IJsonReader
    {
        public static readonly string[] restrictedProperties =
        {
            "objectID",
            "prefabInfos",
            "graphicalPrefab",
            "requiredObjectsToCraft"
        };
        
        public static readonly string[] objectAuthoringSkip =
        {
            "onlyExistsInSeason",
            "sellValue",
            "buyValueMultiplier",
            "icon",
            "iconOffset",
            "smallIcon",
            "isStackable",
            "craftingSettings",
            "requiredObjectsToCraft",
            "craftingTime"
        };
        
        public static readonly string[] inventoryItemSkip =
        {
            "objectID",
            "initialAmount",
            "variation",
            "variationIsDynamic",
            "variationToToggleTo",
            "objectType",
            "tags",
            "rarity",
            "additionalSprites"
        };
        
        public virtual void ApplyPre(JsonElement jObject, FileContext context)
        {
            string itemId = jObject.GetProperty("itemId").GetString();
            GameObject go = new GameObject
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            ObjectAuthoring objectAuthoring = go.AddComponent<ObjectAuthoring>();
            InventoryItemAuthoring itemAuthoring = go.AddComponent<InventoryItemAuthoring>();

            ReadObjectInfo(jObject, objectAuthoring, itemAuthoring);
            ReadComponents(jObject, go);

            ObjectID objectID = EntityModule.AddEntity(itemId, objectAuthoring);

            ReadLocalization(jObject, objectID);
        }
        
        public virtual void ApplyPost(JsonElement jObject, FileContext context)
        {
            ReadRecipes(jObject);
        }

        public static void ReadRecipes(JsonElement jObject)
        {
            string itemId = jObject.GetProperty("itemId").GetString();
            ObjectID objectID = EntityModule.GetObjectId(itemId);

            if (EntityModule.GetMainEntity(objectID, out ObjectAuthoring entity))
            {
                InventoryItemAuthoring itemAuthoring = entity.GetComponent<InventoryItemAuthoring>();
                if (jObject.TryGetProperty("requiredObjectsToCraft", out var itemsElement))
                {
                    List<InventoryItemAuthoring.CraftingObject> recipe = 
                        itemsElement.Deserialize<List<CraftingObject>>(JsonLoaderModule.options)
                            .Select(item => new InventoryItemAuthoring.CraftingObject()
                            {
                                objectID = (int)item.objectID,
                                amount = item.amount
                            }).ToList();
                    itemAuthoring.requiredObjectsToCraft = recipe;
                }

                return;
            }

            throw new InvalidOperationException($"Failed to find item with ID {itemId}!");
        }
        
        public struct CraftingObject
        {
            public ObjectID objectID;
            public int amount;
        }

        public static void ReadComponents(JsonElement jObject, GameObject gameObject)
        {
            if (jObject.TryGetProperty("components", out var componentsElement))
            {
                foreach (var node in componentsElement.EnumerateArray())
                {
                    if (node.TryGetProperty("type", out var typeElement))
                    {
                        Type type = JsonLoaderModule.TypeByName(typeElement.GetString());
                        
                        Component component = gameObject.GetComponent(type);
                        if (component == null)
                            component = gameObject.AddComponent(type);
                        
                        if (component is IHasDefaultValue hasDefaultValue)
                            hasDefaultValue.InitDefaultValues();
                        
                        JsonLoaderModule.PopulateObject(type, component, node);
                        JsonLoaderModule.FillArrays(type, component);
                    }
                }
            }
        }

        public static void ReadObjectInfo(JsonElement jObject, ObjectAuthoring objectAuthoring, InventoryItemAuthoring itemAuthoring)
        {
            string itemId = jObject.GetProperty("itemId").GetString();
            
            JsonLoaderModule.PopulateObject(objectAuthoring, jObject, restrictedProperties, objectAuthoringSkip);
            if (itemAuthoring != null)
                JsonLoaderModule.PopulateObject(itemAuthoring, jObject, restrictedProperties, inventoryItemSkip);

            string fullItemId = $"{itemId}_{objectAuthoring.variation}";
            
            objectAuthoring.gameObject.name = $"{fullItemId}_Prefab";
            JsonLoaderModule.FillArrays(objectAuthoring);
            if (itemAuthoring != null)
                JsonLoaderModule.FillArrays(itemAuthoring);
        }

        public static void ReadLocalization(JsonElement jObject, ObjectID objectID)
        {
            if (objectID == ObjectID.None) return;
            
            void ApplyLocalization(string term, string valueKey)
            {
                if (!jObject.TryGetProperty(valueKey, out var textData)) return;
                
                if (textData.TokenType == JsonTokenType.String)
                {
                    string stringData = textData.GetString();
                    LocalizationModule.AddTerm(term, stringData);
                }
                else if (textData.TokenType == JsonTokenType.StartObject)
                {
                    var dictData = textData.Deserialize<Dictionary<string, string>>(JsonLoaderModule.options);
                    LocalizationModule.AddTerm(term, dictData);
                }
            }
            
            ApplyLocalization($"Items/{objectID}", "localizedName");
            ApplyLocalization($"Items/{objectID}Desc", "localizedDescription");
        }
    }
}