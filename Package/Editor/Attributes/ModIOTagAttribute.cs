using System;
using System.Linq;
using PropertyAttribute = UnityEngine.PropertyAttribute;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    /// Attribute enabling the creation of a multi-selectable dropdown populated by Mod.io tags relevant
    /// to Core Keeper. Tags can be filtered based on specific categories or configuration
    /// (e.g., game versions, access types).
    public class ModIOTagAttribute : PropertyAttribute
    {
        public enum TagKind
        {
            GameVersion,
            Type,
            ApplicationType,
            AdminAction,
            AccessType
        }
        
        public readonly string[] selectedTagKind;
        
        public ModIOTagAttribute(TagKind tagKind = TagKind.GameVersion, bool inverted = false)
        {
            selectedTagKind = Enum.GetValues(typeof(TagKind)).Cast<TagKind>()
                .Where(x => inverted ? x != tagKind : x == tagKind)
                .Select(GetFriendlyString)
                .ToArray();
        }
        
        public ModIOTagAttribute(TagKind[] tagKind, bool inverted = false)
        {
            selectedTagKind = Enum.GetValues(typeof(TagKind)).Cast<TagKind>()
                .Where(x => inverted ? !tagKind.Contains(x) : tagKind.Contains(x))
                .Select(GetFriendlyString)
                .ToArray();
        }
        
        private static string GetFriendlyString(TagKind tagKind)
        {
            return tagKind switch
            {
                TagKind.GameVersion => "Game Version",
                TagKind.Type => "Type",
                TagKind.ApplicationType => "Application Type",
                TagKind.AccessType => "Access Type",
                TagKind.AdminAction => "Admin Action",
                _ => throw new ArgumentOutOfRangeException(nameof(tagKind), tagKind, null)
            };
        }
    }
}