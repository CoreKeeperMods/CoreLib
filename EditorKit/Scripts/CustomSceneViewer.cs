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
        public Material drawMaterial;

        public Mesh boxMesh;
        public Mesh quadMesh;

        [HideInInspector] public string currentScene;
        private bool hasInitialized;

        public static CustomSceneViewer Instance;

        private Dictionary<int3, List<TileView>> tileViews = new Dictionary<int3, List<TileView>>();

        private int viewVersion = -1;
        private int dictVersion = 0;
        private Dictionary<TileGroup, List<TileView>> tileGroups = new Dictionary<TileGroup, List<TileView>>();

        private Dictionary<TileGroup, InstancedRenderer> renderGroups = new Dictionary<TileGroup, InstancedRenderer>();
        private static readonly int baseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int topGenTex = Shader.PropertyToID("_TopGenTex");
        private static readonly int sideGenTex = Shader.PropertyToID("_SideGenTex");

        private void OnValidate()
        {
            Instance = this;
            if (dataTable == null)
                dataTable = Resources.Load<CustomScenesDataTable>("scenes/CustomScenesDataTable");

            if (tilesetBank == null)
                tilesetBank = Resources.Load<MapWorkshopTilesetBank>("mapworkshop/MapWorkshopTilesetBank");
        }

        public void Display()
        {
            ResetState();
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

            foreach (List<TileView> views in tileViews.Values)
            {
                foreach (TileView view in views)
                {
                    view.UpdateVisuals();
                    SetTileAt(view.GetTileGroup(), view);
                }
            }
        }

        private void ResetState()
        {
            tileViews.Clear();
            tileGroups.Clear();
            renderGroups.Clear();
        }

        private void OnDestroy()
        {
            foreach (InstancedRenderer renderer in renderGroups.Values)
            {
                renderer.Destroy();
            }
        }

        private void UpdateRenderers()
        {
            if (viewVersion == dictVersion)
                return;

            foreach (TileGroup group in tileGroups.Keys)
            {
                var tiles = tileGroups[group];
                InstancedRenderer renderer = GetRenderer(group);
                renderer.Update(tiles);
            }

            viewVersion = dictVersion;
        }

        private void Update()
        {
            if (!hasInitialized)
            {
                Display();
                hasInitialized = true;
            }
            
            UpdateRenderers();
            foreach (TileGroup group in tileGroups.Keys)
            {
                GetRenderer(group).Render();
            }
        }

        private InstancedRenderer GetRenderer(TileGroup group)
        {
            if (renderGroups.ContainsKey(group))
            {
                return renderGroups[group];
            }

            Material material = Instantiate(drawMaterial);
            material.SetTexture(baseMap, group.basetex);
            material.SetTexture(topGenTex, group.toptex);
            material.SetTexture(sideGenTex, group.sidetex);
            InstancedRenderer renderer = new InstancedRenderer(material, group.useQuad ? quadMesh : boxMesh, !group.useQuad);
            renderGroups[group] = renderer;
            return renderer;
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
            if (tileViews.ContainsKey(pos))
            {
                tileViews[pos].Add(view);
            }
            else
            {
                tileViews.Add(pos, new List<TileView>()
                {
                    view
                });
            }
        }

        public void SetTileAt(TileGroup group, TileView view)
        {
            if (tileGroups.ContainsKey(group))
            {
                int index = tileGroups[group].IndexOf(view);
                if (index >= 0)
                {
                    tileGroups[group][index] = view;
                }
                else
                {
                    tileGroups[group].Add(view);
                }
            }
            else
            {
                tileGroups.Add(group, new List<TileView>()
                {
                    view
                });
            }

            dictVersion++;
        }

        public byte GetAdjacentTileInfo(int3 pos, TileView current)
        {
            byte mask = 0;
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                int3 newPos = pos + new int3(x, 0, y);
                if (!TryGetTilesAt(newPos, out List<TileView> tiles)) continue;

                if (!tiles.Any(view => current.tileset == view.tileset &&
                                       current.tileType == view.tileType)) continue;

                if (current.tileType == TileType.ground)
                {
                    newPos.y += 1;
                    if (TryGetTilesAt(newPos, out tiles))
                    {
                        if (tiles.Any(view => view.tileType == TileType.wall))
                            continue;
                    }
                }

                mask |= (byte)AdjacentDir.GetDir(new Vector3Int(x, y, 0));
            }

            return mask;
        }

        public bool TryGetTilesAt(int3 pos, out List<TileView> views)
        {
            if (tileViews.ContainsKey(pos))
            {
                views = tileViews[pos];
                return true;
            }

            views = null;
            return false;
        }
    }
}