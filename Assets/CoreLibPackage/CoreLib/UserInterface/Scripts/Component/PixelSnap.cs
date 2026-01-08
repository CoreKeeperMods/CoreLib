using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.UserInterface.Component
{
    /// <summary>
    /// Ensures that UI components or sprites align with a pixel-perfect grid for consistent rendering and precise positioning.
    /// </summary>
    [ExecuteInEditMode]
    public class PixelSnap : MonoBehaviour
    {
        /// <summary>
        /// Represents the size of a single pixel, commonly used for aligning UI elements
        /// to a pixel-perfect grid. Determines the granularity of the snapping behavior
        /// in the <see cref="PixelSnap"/> component.
        /// </summary>
        public int pixelSize = 16;

        /// <summary>
        /// Enables or disables the functionality to snap the size of a sprite to a pixel-perfect grid.
        /// When enabled, the system adjusts the dimensions of the sprite to align with the grid based
        /// on the defined pixel size. This ensures that the sprite maintains alignment with the grid
        /// for consistent visual presentation.
        /// </summary>
        public bool snapSpriteSize;

#if UNITY_EDITOR  
        /// <summary>
        /// Represents the size of the grid used for snapping operations within the <see cref="PixelSnap"/> component.
        /// This defines the horizontal and vertical intervals for aligning positions and resizing elements.
        /// </summary>
        private Vector2 _grid;

        /// <summary>
        /// Draws Gizmos in the Scene view to visualize the pixel-perfect grid alignment defined by the component.
        /// This is primarily used in the Unity Editor to provide immediate feedback about how objects
        /// are being aligned and snapped to the grid.
        /// </summary>
        private void OnDrawGizmos()
        {
            SnapToGrid();
        }

        /// <summary>
        /// Adjusts the position and size of the GameObject to align with a pixel-perfect grid.
        /// The method calculates the nearest grid-aligned position based on the current
        /// position and updates it accordingly. Additionally, if sprite size snapping is enabled,
        /// it aligns the sprite dimensions to the same grid while updating associated colliders.
        /// </summary>
        private void SnapToGrid()
        {
            if (_grid.x == 0 || _grid.y == 0) return;

            Vector3 position = transform.localPosition;

            Vector3 newPos = new Vector3(
                Mathf.Round(position.x / _grid.x) * _grid.x,
                Mathf.Round(position.y / _grid.y) * _grid.y,
                position.z);

            transform.localPosition = newPos;

            if (snapSpriteSize)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr == null) return;

                Vector2 size = sr.size;

                Vector2 newSize = new Vector2(
                    Mathf.Round(size.x / _grid.x) * _grid.x,
                    Mathf.Round(size.y / _grid.y) * _grid.y);

                var srCollider = GetComponent<BoxCollider>();
                if (srCollider != null)
                {
                    srCollider.center = Vector3.zero;
                    srCollider.size = new Vector3(newSize.x, newSize.y, 0.1f);
                }

                sr.size = newSize;
            }
        }

        /// <summary>
        /// Validates and recalculates the pixel snapping grid whenever changes are made to the component's
        /// properties in the Unity Inspector. Ensures that the grid values are properly updated
        /// based on the defined pixel size for consistent pixel-perfect snapping functionality.
        /// </summary>
        private void OnValidate()
        {
            var snap = 1 / (float)pixelSize;
            _grid = new Vector2(snap, snap);
        }
#else
        private void Awake()
        {
            Destroy(this);
        }
#endif
    }
}