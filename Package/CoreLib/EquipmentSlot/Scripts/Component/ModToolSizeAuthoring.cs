using UnityEngine;

namespace CoreLib.Submodule.EquipmentSlot.Component
{

    /// Authoring to set prefabTileSize for modded tools that want the tool to apply to more tiles at once
    public class ModToolSizeAuthoring : MonoBehaviour
    {
        public Vector2Int prefabTileSize = Vector2Int.one;
    }
}