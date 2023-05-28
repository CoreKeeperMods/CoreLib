using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreLibKit;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;
using Unity.Mathematics;
using UnityEngine;

namespace EditorKit.Scripts
{
    [ExecuteInEditMode]
    public class CustomSceneViewer : MonoBehaviour
    {
        public CustomScenesDataTable dataTable;
        public MapWorkshopTilesetBank tilesetBank;
        public GameObject displayRoot;
        public Transform mapsRoot;

        public MapChunkView MapPrefab;

        public Mesh boxMesh;
        public Mesh skewedMesh;
        public Mesh skewedMesh2;

        [HideInInspector] public string currentScene;

        public MaterialPropertyBlock propertyBlock;
        public static CustomSceneViewer Instance;
        
        private Dictionary<int3, TileView> tileViews = new Dictionary<int3, TileView>();

        private void OnValidate()
        {
            Instance = this;
            if (dataTable == null)
                dataTable = Resources.Load<CustomScenesDataTable>("scenes/CustomScenesDataTable");

            if (tilesetBank == null)
                tilesetBank = Resources.Load<MapWorkshopTilesetBank>("mapworkshop/MapWorkshopTilesetBank");

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
        }

        public void Display()
        {
            tileViews.Clear();
            displayRoot.ClearChildren();
            mapsRoot.gameObject.ClearChildren();

            CustomScenesDataTable.Scene scene = dataTable.scenes.First(scene1 => scene1.sceneName.Equals(currentScene));

            for (int i = 0; i < scene.prefabs.Count; i++)
            {
                GameObject prefab = scene.prefabs[i];
                Vector3 pos = scene.prefabPositions[i];

                GameObject spawned = Instantiate(prefab, pos, Quaternion.identity, displayRoot.transform);
                EntityMonoBehaviourData data = spawned.GetComponent<EntityMonoBehaviourData>();

                foreach (PrefabInfo info in data.objectInfo.prefabInfos)
                {
                    if (info.prefab == null) continue;
                    EntityMonoBehaviour prefabInst = Instantiate(info.prefab, spawned.transform);
                    prefabInst.UpdateGraphicsFromObjectInfo(data.objectInfo);
                }
            }

            foreach (CustomScenesDataTable.Map map in scene.maps)
            {
                MapChunkView root = Instantiate(MapPrefab, new Vector3(map.localPosition.x, 0, map.localPosition.y), Quaternion.identity, mapsRoot);
                root.SetMap(map);
            }

            foreach (TileView view in tileViews.Values)
            {
                view.UpdateVisuals();
            }
        }

        public MapWorkshopTilesetBank.Tileset GetTileset(int tileset)
        {
            if (tileset < tilesetBank.tilesets.Count)
            {
                return tilesetBank.tilesets[tileset];
            }

            return null;
        }

        public void SetTileAt(int3 pos, TileView view)
        {
            tileViews.TryAdd(pos, view);
        }

        public byte GetAdjacentTileInfo(int3 pos, TileView current)
        {
            byte mask = 0;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    
                    int3 newPos = pos + new int3(x, 0, y);
                    if (!TryGetTileAt(newPos, out TileView tile)) continue;
                    
                    if (current.tileset != tile.tileset ||
                        current.tileType != tile.tileType) continue;
                        
                    mask |= (byte)AdjacentDir.GetDir(new Vector3Int(x, y, 0));
                }
            }

            return mask;
        }

        public bool TryGetTileAt(int3 pos, out TileView view)
        {
            if (tileViews.ContainsKey(pos))
            {
                view = tileViews[pos];
                return true;
            }

            view = null;
            return false;
        }
    }
}