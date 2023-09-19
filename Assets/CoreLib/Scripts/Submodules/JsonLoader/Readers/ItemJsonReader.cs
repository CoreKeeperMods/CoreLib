using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CoreLib.Submodules.JsonLoader.Components;
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
            "graphicalPrefab"
        };
        
        public static readonly Type[] objectAuthoringSet =
        {
            typeof(ObjectAuthoring),
            typeof(InventoryItemAuthoring),
            typeof(PlaceableObjectAuthoring)
        };

        public virtual void ApplyPre(JsonElement jObject, FileContext context)
        {
            string itemId = jObject.GetProperty("itemId").GetString();
            GameObject go = new GameObject
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            ObjectAuthoring objectAuthoring = go.AddComponent<ObjectAuthoring>();
            go.AddComponent<InventoryItemAuthoring>();
            
            if (jObject.TryGetProperty("prefabTileSize", out _))
                go.AddComponent<PlaceableObjectAuthoring>();

            ReadObjectInfo(jObject, objectAuthoring);
            ReadComponents(jObject, go);
            
            ReadLocalization(jObject, go, itemId);
            
            EntityModule.AddEntity(itemId, objectAuthoring);
        }
        
        public virtual void ApplyPost(JsonElement jObject, FileContext context)
        {
            ReadRecipes(jObject);
        }

        public static void ReadRecipes(JsonElement jObject)
        {
            string itemId = jObject.GetProperty("itemId").GetString();
            
            if (EntityModule.GetMainEntity(itemId, out ObjectAuthoring entity))
            {
                InventoryItemAuthoring itemAuthoring = entity.GetComponent<InventoryItemAuthoring>();
                if (jObject.TryGetProperty("requiredObjectsToCraft", out var itemsElement))
                {
                    List<InventoryItemAuthoring.CraftingObject> recipe = 
                        itemsElement.Deserialize<List<InventoryItemAuthoring.CraftingObject>>(JsonLoaderModule.options);
                    itemAuthoring.requiredObjectsToCraft = recipe;
                }

                return;
            }

            throw new InvalidOperationException($"Failed to find item with ID {itemId}!");
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

                        if (node.TryGetProperty("$data", out var dataElement) &&
                            dataElement.TokenType == JsonTokenType.StartObject)
                        {
                            JsonLoaderModule.PopulateObject(type, component, dataElement);
                        }
                        else
                        {
                            JsonLoaderModule.PopulateObject(type, component, node);
                        }
                        
                        JsonLoaderModule.FillArrays(type, component);
                    }
                }
            }
        }

        public static void ReadObjectInfo(JsonElement jObject, ObjectAuthoring objectAuthoring)
        {
            string itemId = jObject.GetProperty("itemId").GetString();
            var itemAuthoring = objectAuthoring.GetComponent<InventoryItemAuthoring>();
            var placeablePrefab = objectAuthoring.GetComponent<PlaceableObjectAuthoring>();

            var typeSet = objectAuthoringSet.Where(type => objectAuthoring.GetComponent(type) != null).ToArray();
            
            JsonLoaderModule.PopulateObject(objectAuthoring, jObject, restrictedProperties, typeSet);
            JsonLoaderModule.PopulateObject(itemAuthoring, jObject, restrictedProperties, typeSet);
            JsonLoaderModule.PopulateObject(placeablePrefab, jObject, restrictedProperties, typeSet);

            string fullItemId = $"{itemId}_{objectAuthoring.variation}";
            
            objectAuthoring.gameObject.name = $"{fullItemId}_Prefab";
            JsonLoaderModule.FillArrays(objectAuthoring);
            JsonLoaderModule.FillArrays(itemAuthoring);
            JsonLoaderModule.FillArrays(placeablePrefab);
        }

        public static void ReadLocalization(JsonElement jObject, GameObject entity, string itemId)
        {
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
            
            var localization = entity.AddComponent<LocalizationAuthoring>();
            localization.termKey = $"Items/{itemId}";
            
            ApplyLocalization(localization.termKey, "localizedName");
            ApplyLocalization($"{localization.termKey}Desc", "localizedDescription");
        }
    }
}