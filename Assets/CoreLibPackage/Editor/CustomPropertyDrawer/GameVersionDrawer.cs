using System;
using System.Collections.Generic;
using System.Linq;
using ModIO;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    [CustomPropertyDrawer(typeof(ModIOTagAttribute))]
    public class GameVersionDrawer : PropertyDrawer
    {
        private TagCategory[] _allTags = Array.Empty<TagCategory>();
        private bool _initialized = true;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                var errorColor = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.2f, 0.2f) } };
                EditorGUI.DropShadowLabel(position, new GUIContent($"*ModIOTagAttribute* used non-string property: {property.displayName}"), errorColor);
                return;
            }

            if (_initialized)
            {
                var options = new HashSet<string>(property.stringValue.Split(';'));
                property.stringValue = string.Join(";", options.OrderBy(n => n));
            }
            
            EditorGUI.BeginProperty(position, label, property);
            {
                var newPosition = EditorGUI.PrefixLabel(position, label);
                
                if (_allTags.Length == 0 && ModIOExtensions.InitializeModIO())
                    _allTags = ModIOExtensions.FetchTags();
                
                var selectedOptions = new HashSet<string>();
                if (!string.IsNullOrEmpty(property.stringValue))
                    selectedOptions = new HashSet<string>(property.stringValue.Split(';'));

                var buttonText = new GUIContent(property.stringValue == "" ? "Select Options" : property.stringValue);
                
                if (EditorGUI.DropdownButton(newPosition, buttonText, FocusType.Passive))
                {
                    var modIOTag = (ModIOTagAttribute)attribute;
            
                    if (_allTags.Length == 0) return;
                    var allTags = _allTags.Where(x => modIOTag.selectedTagKind.Contains(x.name) && x.hidden != true).ToArray();
                    if (allTags.Length == 0) return;

                    var menu = new GenericMenu();
                    foreach (var category in allTags)
                    {
                        foreach (var tag in category.tags)
                        {
                            bool isSelected = selectedOptions.Contains(tag.name);
                            string option = category.name == "Game Version" ? $"v{tag.name[..tag.name.IndexOf('.')]}.0/{tag.name}" : $"{category.name}/{tag.name}";

                            menu.AddItem(new GUIContent(option), isSelected, () =>
                            {
                                if (isSelected)
                                    selectedOptions.Remove(tag.name);
                                else
                                    selectedOptions.Add(tag.name);
                                property.stringValue = string.Join(";", selectedOptions);
                                property.serializedObject.ApplyModifiedProperties();
                            });
                        }
                    }
                    menu.ShowAsContext();
                }
            }
            EditorGUI.EndProperty();
        }
    }
}