using UnityEngine;
#pragma warning disable CS0108, CS0114

namespace EditorKit.Scripts
{
    [ExecuteInEditMode]
    public class CameraSkew : MonoBehaviour
    {
        private Matrix4x4 originalProjection;
        private Camera camera;

        public bool enableSkew;
        [Range(0f, 90f)]
        public float outputSkewAngle = 45f;

        private float lastAngle;
        private float lastOrtoSize;
        

        void Start()
        {
            camera = GetComponent<Camera>();
            originalProjection = camera.projectionMatrix;
            lastOrtoSize = camera.orthographicSize;
        }

        void Update()
        {
            if (!enableSkew)
            {
                lastAngle = 0;
                camera.projectionMatrix = originalProjection;
                return;
            }

            if (Mathf.Abs(lastOrtoSize - camera.orthographicSize) > 0)
            {
                originalProjection = camera.projectionMatrix;
                lastOrtoSize = camera.orthographicSize;
                lastAngle = 0;
            }
            
            if (Mathf.Abs(lastAngle - outputSkewAngle) > 0.1f)
            {
                if (camera.orthographic && enableSkew && outputSkewAngle > 0f)
                {
                    Matrix4x4 mat = originalProjection;
                    mat[1, 1] /= Mathf.Cos(outputSkewAngle * 0.017453292f);
                    camera.projectionMatrix = mat;
                }

                lastAngle = outputSkewAngle;
            }
        }
    }
}