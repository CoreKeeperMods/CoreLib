using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.TileSet.Attribute
{
    /// A custom property attribute used to annotate fields that reference tileset configurations in Unity.
    /// <remarks>
    /// This attribute is intended to be placed on fields, such as strings, to indicate that the field
    /// refers to a tileset. It can be utilized with custom editor tools to provide enhanced functionality,
    /// such as dropdown menus for selecting tilesets or validation during editing.
    /// </remarks>
    public class ModTilesetAttribute : PropertyAttribute
    {
    }
}