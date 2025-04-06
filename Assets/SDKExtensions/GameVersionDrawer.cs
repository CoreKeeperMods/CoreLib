using System;
using System.Collections.Generic;
using System.Linq;
using ModIO;
using UnityEditor;
using UnityEngine;

namespace CoreLib.Editor
{
    [CustomPropertyDrawer(typeof(ModIOTagAttribute))]
    public class GameVersionDrawer : PropertyDrawer
    {
        private string clickName;
        private Tag lastClick;
        private bool didClick;

        private static TagCategory[] allTags = Array.Empty<TagCategory>();
        private static bool triedToFetch = false;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "GameVersionsAttribute used on non-string type.");
                return;
            }
            
            EditorGUI.BeginProperty(position, label, property);

            if (allTags.Length == 0 && ChainBuilderEditor.InitializeModIO())
            {
                FetchTags();
            }
            
            ModIOTagAttribute modIOTag = (ModIOTagAttribute)attribute;

            Rect labelRect = new Rect(position.x, position.y, position.width * 0.4f, position.height);
            Rect dropDownRect = new Rect(position.x + position.width * 0.4f, position.y, position.width * 0.6f, position.height);

            EditorGUI.LabelField(labelRect, label);
            
            string currentValue = property.stringValue;
            string[] tags = currentValue.Split(';');

            if (EditorGUI.DropdownButton(dropDownRect, new GUIContent(currentValue), FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                var matchCount = allTags.Count(category => modIOTag.Matches(category));
                
                foreach (var category in allTags)
                {
                    if (!modIOTag.Matches(category)) continue;
                    
                    foreach (Tag tag in category.tags)
                    {
                        var on = tags.Any(tagStr => tagStr == tag.name);
                        string itemName = tag.name;
                        if (matchCount > 1)
                            itemName = $"{category.name}/{tag.name}";
                        else if (category.name == "Game Version")
                        {
                            if (tag.name.StartsWith("0"))
                                itemName = $"Legacy/{tag.name}";
                        }
                        
                        menu.AddItem(new GUIContent(itemName), on, data =>
                        {
                            lastClick = (Tag)data;
                            clickName = property.propertyPath;
                            didClick = true;
                        }, tag);
                    }
                }
                
                menu.ShowAsContext();
            }

            if (didClick && property.propertyPath.Equals(clickName))
            {
                if (tags.Any(tag => tag == lastClick.name))
                {
                    var newTags = tags.Where(tag => tag != lastClick.name).Where(s => s != "");
                    property.stringValue = string.Join(';', newTags);
                }
                else
                {
                    var newTags = tags.Append(lastClick.name).Where(s => s != "");
                    property.stringValue = string.Join(';', newTags);
                }
                
                didClick = false;
            }
        }

        private static void FetchTags()
        {
            if (triedToFetch) return;
            
            ModIOUnity.GetTagCategories(result =>
            {
                if (result.result.errorCode == 20303 || !result.result.Succeeded())
                {
                    Debug.LogError($"failed to fetch categories data");
                    return;
                }

                allTags = result.value;
                Debug.Log(string.Join(", ", allTags.Select(t => t.name)));
            });
            triedToFetch = true;
        }
    }
}