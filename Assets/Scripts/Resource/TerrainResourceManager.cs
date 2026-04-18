using System;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LushWorld.Resource
{
    // Scene-level manager (NOT inside PlayerRig). Reads terrain tree positions at runtime,
    // removes those trees to avoid visual overlap, and streams interactive ResourceNode
    // prefabs near the player. Only ~5-10 rocks are active GameObjects at once.
    //
    // Runtime tree removal only — the .asset on disk is never modified.
    public class TerrainResourceManager : MonoBehaviour
    {
        [Serializable]
        public struct ResourcePrototypeEntry
        {
            public int prototypeIndex;
            public GameObject interactivePrefab;
        }

        [SerializeField] private ResourcePrototypeEntry[] resourcePrototypes;
        [SerializeField] private float spawnRadius = 25f;
        [SerializeField] private float despawnRadius = 30f;
        [SerializeField] private Transform playerTransform;

        private class ResourceSpot
        {
            public Vector3 worldPos;
            public int protoIndex;
            public bool harvested;
            public GameObject activeGO;
            public TreeInstance originalInstance;
        }

        private readonly List<ResourceSpot> _spots = new List<ResourceSpot>();
        private Terrain _terrain;
        private TreeInstance[] _originalTreeInstances;
        private float _updateTimer;
        private const float UpdateInterval = 0.5f;

        private void Awake()
        {
            _terrain = Terrain.activeTerrain;
            if (_terrain == null)
            {
                Debug.LogWarning("[TerrainResourceManager] No active terrain found.");
                return;
            }

            // Cache before any modifications so OnDestroy can fully restore
            _originalTreeInstances = _terrain.terrainData.treeInstances;
            BuildSpotList();

            Debug.Log($"[TerrainResourceManager] Found {_spots.Count} resource spots on terrain.");
            if (_spots.Count == 0)
                Debug.LogWarning("[TerrainResourceManager] No resource spots found. " +
                    "Check that 'Resource Prototypes' prototypeIndex values match your terrain's tree prototype indices " +
                    "(0-based order in the Terrain Inspector under Paint Trees).");

            if (_terrain.treeDistance < despawnRadius * 2f)
                Debug.LogWarning($"[TerrainResourceManager] Terrain treeDistance ({_terrain.treeDistance}) is too small — " +
                    $"rocks will be culled by Unity before they reach swap range ({despawnRadius}u). " +
                    $"Fix: Select the Terrain → Inspector → Terrain Settings → Tree & Detail Objects → " +
                    $"set Tree Distance to at least {despawnRadius * 4f}.");

            if (playerTransform == null)
            {
                var fpc = FindFirstObjectByType<FirstPersonController>();
                if (fpc != null)
                    playerTransform = fpc.transform;
                else
                    Debug.LogWarning("[TerrainResourceManager] playerTransform not assigned and FirstPersonController not found. Rocks will not spawn.");
            }

            // Resolve to actual moving object if playerTransform is a parent container (e.g. PlayerRig).
            // CharacterController is always on the real moving child (PlayerCapsule).
            if (playerTransform != null)
            {
                var cc = playerTransform.GetComponentInChildren<CharacterController>();
                if (cc != null && cc.transform != playerTransform)
                {
                    Debug.Log($"[TerrainResourceManager] playerTransform resolved from '{playerTransform.name}' to CharacterController on '{cc.gameObject.name}'");
                    playerTransform = cc.transform;
                }
            }

#if UNITY_EDITOR
            // Guard against interrupted play sessions: restores terrain even if OnDestroy is skipped.
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                RestoreTerrainTrees();
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            }
        }
#endif

        private void OnDestroy()
        {
            RestoreTerrainTrees();
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
        }

        private void RestoreTerrainTrees()
        {
            if (_terrain == null || _originalTreeInstances == null) return;
            _terrain.terrainData.treeInstances = _originalTreeInstances;
            _originalTreeInstances = null; // prevent double-restore
        }

        private void BuildSpotList()
        {
            var td = _terrain.terrainData;
            var instances = td.treeInstances;
            var protoSet = BuildProtoIndexSet();

            foreach (var ti in instances)
            {
                if (!protoSet.Contains(ti.prototypeIndex)) continue;

                Vector3 worldPos = Vector3.Scale(ti.position, td.size) + _terrain.transform.position;
                // ti.position.y is always 0 (trees sit on the surface); sample actual terrain height.
                worldPos.y = _terrain.SampleHeight(worldPos) + _terrain.transform.position.y;
                _spots.Add(new ResourceSpot
                {
                    worldPos = worldPos,
                    protoIndex = ti.prototypeIndex,
                    harvested = false,
                    activeGO = null,
                    originalInstance = ti
                });
            }
            // Trees stay in the terrain here — each one is removed lazily in SpawnSpot()
            // so distant rocks remain visible as terrain billboards until approached.
        }

        private HashSet<int> BuildProtoIndexSet()
        {
            var set = new HashSet<int>();
            if (resourcePrototypes == null) return set;
            foreach (var entry in resourcePrototypes)
                set.Add(entry.prototypeIndex);
            return set;
        }

        private void Update()
        {
            if (playerTransform == null || _spots.Count == 0) return;

            _updateTimer -= Time.deltaTime;
            if (_updateTimer > 0f) return;
            _updateTimer = UpdateInterval;

            Vector3 playerPos = playerTransform.position;
            float spawnSq = spawnRadius * spawnRadius;
            float despawnSq = despawnRadius * despawnRadius;

            foreach (var spot in _spots)
            {
                if (spot.harvested) continue;

                float distSq = Vector3.SqrMagnitude(spot.worldPos - playerPos);

                if (distSq <= spawnSq && spot.activeGO == null)
                {
                    SpawnSpot(spot);
                }
                else if (distSq > despawnSq && spot.activeGO != null)
                {
                    DespawnSpot(spot);
                }
            }
        }

        private void SpawnSpot(ResourceSpot spot)
        {
            var prefab = GetPrefabForProto(spot.protoIndex);
            if (prefab == null)
            {
                Debug.LogWarning($"[TerrainResourceManager] SpawnSpot: no prefab mapped for prototypeIndex {spot.protoIndex}. Rock at {spot.worldPos} will stay as terrain tree forever.");
                return;
            }

            // Remove this individual tree now so the terrain billboard and the prefab
            // never overlap. All other distant trees remain visible until their own turn.
            RemoveTreeInstance(spot.originalInstance);
            spot.activeGO = Instantiate(prefab, spot.worldPos, Quaternion.identity);
            Debug.Log($"[TerrainResourceManager] Spawned '{prefab.name}' at {spot.worldPos} | scale ws={spot.originalInstance.widthScale:F2} hs={spot.originalInstance.heightScale:F2}");

            // Read the prefab's authored LODGroup size while localScale is still (1,1,1).
            // We need this reference before we change localScale so we can correct it afterward.
            var lodGroup = spot.activeGO.GetComponentInChildren<LODGroup>();
            float authoredLodSize = (lodGroup != null) ? lodGroup.size : 0f;

            // Apply terrain tree scale. Clamp to a minimum of 1.0 so rocks never shrink
            // below their authored design size. A widthScale < 1 (e.g. 0.44) produces
            // sub-centimetre pebbles that are invisible from any normal gameplay distance;
            // this happens when the terrain brush Width slider was left below 1.0.
            float ws = Mathf.Max(spot.originalInstance.widthScale, 1f);
            float hs = Mathf.Max(spot.originalInstance.heightScale, 1f);

            if (spot.originalInstance.widthScale < 1f)
                Debug.LogWarning($"[TerrainResourceManager] Rock at {spot.worldPos} has widthScale={spot.originalInstance.widthScale:F2} (<1). " +
                    "Rocks will be invisible. Fix: select the Terrain, open Paint Trees, click your rock prototype, and set Width to at least 1.");

            spot.activeGO.transform.localScale = new Vector3(ws, hs, ws);

            // LODGroup.size is a world-space reference diameter that does NOT update automatically
            // when the transform's localScale is changed. Scale it proportionally so LOD transition
            // distances stay calibrated to the actual visual size of the spawned rock.
            if (lodGroup != null && authoredLodSize > 0f)
                lodGroup.size = authoredLodSize * ws;

            var node = spot.activeGO.GetComponent<ResourceNode>();
            if (node != null)
                node.OnPickedUp += OnNodePickedUp;
        }

        private void DespawnSpot(ResourceSpot spot)
        {
            if (spot.activeGO != null)
            {
                var node = spot.activeGO.GetComponent<ResourceNode>();
                if (node != null)
                    node.OnPickedUp -= OnNodePickedUp;

                Destroy(spot.activeGO);
                spot.activeGO = null;

                // Re-add the tree instance so the rock reappears at distance
                AddTreeInstance(spot.originalInstance);
            }
        }

        private void OnNodePickedUp(ResourceNode node)
        {
            foreach (var spot in _spots)
            {
                if (spot.activeGO != node.gameObject) continue;
                spot.harvested = true;
                spot.activeGO = null; // Destroy() already called by ResourceNode
                return;
            }
        }

        private void RemoveTreeInstance(TreeInstance ti)
        {
            if (_terrain == null) return;
            var td = _terrain.terrainData;
            var current = td.treeInstances;
            var updated = new List<TreeInstance>(current.Length);
            bool removed = false;
            foreach (var t in current)
            {
                // Match by normalized position (0–1 range on terrain); 1e-6 tolerance is ~1mm on a 1000u terrain.
                if (!removed && Vector3.SqrMagnitude(t.position - ti.position) < 1e-6f)
                {
                    removed = true;
                    continue;
                }
                updated.Add(t);
            }
            if (removed)
                td.treeInstances = updated.ToArray();
            else
                Debug.LogWarning($"[TerrainResourceManager] RemoveTreeInstance: no terrain tree matched normalized pos {ti.position}. " +
                    "Terrain billboard may remain visible underneath the spawned prefab.");
        }

        private void AddTreeInstance(TreeInstance ti)
        {
            if (_terrain == null) return;
            var td = _terrain.terrainData;
            var current = td.treeInstances;
            var updated = new TreeInstance[current.Length + 1];
            current.CopyTo(updated, 0);
            updated[current.Length] = ti;
            td.treeInstances = updated;
        }

        private GameObject GetPrefabForProto(int protoIndex)
        {
            if (resourcePrototypes == null) return null;
            foreach (var entry in resourcePrototypes)
                if (entry.prototypeIndex == protoIndex)
                    return entry.interactivePrefab;
            return null;
        }
    }
}
