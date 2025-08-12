using ModIO;
using UnityEngine;

namespace CoreLib.Editor
{
    /// <summary>
    ///   <para>Attribute to make a multi-selectable dropdown with all Core Keeper Game Versions from Mod.io.</para>
    /// </summary>
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

        public TagKind kind = TagKind.GameVersion;
        public bool inverted = false;

        public bool Matches(TagCategory tagCategory)
        {
            if (tagCategory.hidden) return false;
            
            var name = tagCategory.name.Replace(" ", "");
            return inverted ? kind.ToString() != name : kind.ToString() == name;
        }
    }
}