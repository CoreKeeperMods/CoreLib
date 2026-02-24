using System;
using UnityEngine;

namespace CoreLib.Util
{
    [Serializable]
    public struct MarkerData
    {
        public bool isLine;

        public GameObject gameObject;
        public MeshRenderer meshRenderer;
        public MeshRenderer quadRenderer;

        public MarkerData(GameObject gameObject, MeshRenderer meshRenderer, MeshRenderer quadRenderer)
        {
            this.gameObject = gameObject;
            this.meshRenderer = meshRenderer;
            this.quadRenderer = quadRenderer;

            isLine = this.quadRenderer != null;
        }

        public void Set(Vector3 pos, Color color)
        {
            if (isLine) return;

            meshRenderer.material.color = color;
            gameObject.transform.localPosition = pos;
            gameObject.SetActive(true);
        }

        public void SetLine(Vector3 pos1, Vector3 pos2, Color color)
        {
            if (!isLine) return;

            quadRenderer.material.color = color;

            var dist = Vector3.Distance(pos1, pos2);
            var mid = (pos1 + pos2) / 2;
            var dir = (pos2 - pos1).normalized;
            var rotation = Vector2.SignedAngle(Vector2.right, new Vector2(dir.x, dir.z));

            gameObject.transform.localPosition = mid;
            gameObject.transform.localScale = new Vector3(dist, 0.1f, 1);
            gameObject.transform.localRotation = Quaternion.Euler(-90, -rotation, 0);

            gameObject.SetActive(true);
        }

        public void Clear()
        {
            gameObject.SetActive(false);
        }
    }
}