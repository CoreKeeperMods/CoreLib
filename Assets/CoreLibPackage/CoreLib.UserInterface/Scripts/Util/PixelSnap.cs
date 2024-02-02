using UnityEngine;

namespace CoreLib.UserInterface.Util
{
    /// <summary>
    /// Snap UI elements to a pixel grid
    /// </summary>
    [ExecuteInEditMode]
    public class PixelSnap : MonoBehaviour
    {
        public int pixelSize = 16;
        public bool snapSpriteSize;

        private Vector2 grid;

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            SnapToGrid();
        }

        private void SnapToGrid()
        {
            if (grid.x == 0 || grid.y == 0) return;

            Vector3 position = transform.localPosition;

            Vector3 newPos = new Vector3(
                Mathf.Round(position.x / grid.x) * grid.x,
                Mathf.Round(position.y / grid.y) * grid.y,
                position.z);

            transform.localPosition = newPos;

            if (snapSpriteSize)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr == null) return;

                Vector2 size = sr.size;

                Vector2 newSize = new Vector2(
                    Mathf.Round(size.x / grid.x) * grid.x,
                    Mathf.Round(size.y / grid.y) * grid.y);

                var srCollider = GetComponent<BoxCollider>();
                if (srCollider != null)
                {
                    srCollider.center = Vector3.zero;
                    srCollider.size = new Vector3(newSize.x, newSize.y, 0.1f);
                }

                sr.size = newSize;
            }
        }

        private void OnValidate()
        {
            var snap = 1 / (float)pixelSize;
            grid = new Vector2(snap, snap);
        }
#else
        private void Awake()
        {
            Destroy(this);
        }
#endif
    }
}