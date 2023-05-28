using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EditorKit.Scripts
{
    public class InstancedRenderer
    {
        public Material material;
        public Mesh mesh;
        private bool castShadows;

        public InstancedRenderer(Material material, Mesh mesh, bool castShadows)
        {
            this.castShadows = castShadows;
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.material = material;
            this.mesh = mesh;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Destroy;
        }
        
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        private ComputeBuffer argsBuffer;
        private ComputeBuffer renderBuffer;

        private int initialSize = 128;
        private static readonly int instanceBuffer = Shader.PropertyToID("instanceBuffer");

        public void Update(List<TileView> tiles)
        {
            InitBuffer(tiles.Count);

            TileView.MeshData[] meshDatas = new TileView.MeshData[renderBuffer.count];
            for (int i = 0; i < tiles.Count; i++)
            {
                if (i < tiles.Count)
                {
                    meshDatas[i] = tiles[i].meshData;
                    meshDatas[i].instId = i;
                }
                else
                {
                    meshDatas[i].instId = 0;
                }
            }
            renderBuffer.SetData(meshDatas);
            material.SetBuffer(instanceBuffer, renderBuffer);
            
            args[0] = mesh.GetIndexCount(0);
            args[1] = (uint)tiles.Count;
            args[2] = mesh.GetIndexStart(0);
            args[3] = mesh.GetBaseVertex(0);
            
            argsBuffer.SetData(args);
        }

        public void Render()
        {
            Graphics.DrawMeshInstancedIndirect(
                mesh, 
                0, 
                material, 
                new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), 
                argsBuffer,
                castShadows: castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off);
        }

        public void Destroy()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Destroy;
            argsBuffer?.Release();
            renderBuffer?.Release();
        }

        private void InitBuffer(int needCount)
        {
            if (renderBuffer == null)
            {
                while (initialSize < needCount)
                    initialSize *= 2;

                renderBuffer = new ComputeBuffer(initialSize, 52);
            }

            if (renderBuffer.count < needCount)
            {
                int size = renderBuffer.count;
                while (size < needCount)
                    size *= 2;

                renderBuffer.Release();
                renderBuffer = new ComputeBuffer(size, 52);
            }
        }
    }
}