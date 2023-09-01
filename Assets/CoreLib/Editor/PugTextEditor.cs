using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace EditorKit.Editor
{
    [CustomEditor(typeof(PugText))]
    public class PugTextEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            PugText text = (PugText)target;
            
            /*
             * 
  public bool renderOnStart;
  public bool keepEnabledOnStart;
  public bool alwaysUpdateDynamicTextPixelPos;
  public bool isWrittenToByUser;
  public bool isHidden;
  public bool trackDynamicTextCharacterEndPositions;
  public bool localize;
  
  public List<SystemLanguage> languagesToForceLatinFont;
  
  public bool localizePlaceholders = true;
  public bool dontResetEffectsOnRender;
  public bool hideInStreamerMode;
  
  public float maxWidth;
  
  [Multiline]
  public string textString = "Core Keeper";
  public string[] formatFields = new string[0];
  
  public Material overrideMaterial;
  public string offsetKey;
  public PugTextStyle style = new PugTextStyle();

  public List<PugText.StyleOverride> styleOverrides;
             */
            
            EditorGUI.BeginChangeCheck();

            text.renderOnStart = EditorGUILayout.Toggle(SplitCamelCase(nameof(PugText.renderOnStart)), text.renderOnStart);
            text.keepEnabledOnStart = EditorGUILayout.Toggle(SplitCamelCase(nameof(PugText.keepEnabledOnStart)), text.keepEnabledOnStart);
            text.alwaysUpdateDynamicTextPixelPos = EditorGUILayout.Toggle(SplitCamelCase(nameof(PugText.alwaysUpdateDynamicTextPixelPos)), text.alwaysUpdateDynamicTextPixelPos);
            text.isWrittenToByUser = EditorGUILayout.Toggle(SplitCamelCase(nameof(PugText.isWrittenToByUser)), text.isWrittenToByUser);
            text.isHidden = EditorGUILayout.Toggle(SplitCamelCase(nameof(PugText.isHidden)), text.isHidden);
            text.trackDynamicTextCharacterEndPositions = EditorGUILayout.Toggle(SplitCamelCase(nameof(PugText.trackDynamicTextCharacterEndPositions)), text.trackDynamicTextCharacterEndPositions);
            text.localize = EditorGUILayout.Toggle(SplitCamelCase(nameof(PugText.localize)), text.localize);
            
            text.maxWidth = EditorGUILayout.FloatField(SplitCamelCase(nameof(PugText.maxWidth)), text.maxWidth);
            
            text.localizePlaceholders = EditorGUILayout.Toggle(SplitCamelCase(nameof(PugText.localizePlaceholders)), text.localizePlaceholders);
            text.dontResetEffectsOnRender = EditorGUILayout.Toggle(SplitCamelCase(nameof(PugText.dontResetEffectsOnRender)), text.dontResetEffectsOnRender);
            text.hideInStreamerMode = EditorGUILayout.Toggle(SplitCamelCase(nameof(PugText.hideInStreamerMode)), text.hideInStreamerMode);
            
            text.textString = EditorGUILayout.TextField(SplitCamelCase(nameof(PugText.textString)), text.textString);

            text.overrideMaterial = (Material)EditorGUILayout.ObjectField(SplitCamelCase(nameof(PugText.overrideMaterial)), text.overrideMaterial, typeof(Material), false);
            text.offsetKey = EditorGUILayout.TextField(SplitCamelCase(nameof(PugText.offsetKey)), text.offsetKey);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(text);
            }
        }
        
        public static string SplitCamelCase( string str )
        {
            return Regex.Replace( 
                Regex.Replace( 
                    str, 
                    @"(\P{Ll})(\P{Ll}\p{Ll})", 
                    "$1 $2" 
                ), 
                @"(\p{Ll})(\P{Ll})", 
                "$1 $2" 
            );
        }
    }
}