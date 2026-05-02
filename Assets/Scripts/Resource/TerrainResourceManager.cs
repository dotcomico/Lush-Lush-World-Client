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
            public GameObject treePrefab;          // same prefab used in Terrain → Paint Trees
            public GameObject interactivePrefab;   // interactive pickup prefab to spawn near player
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
        private Dictionary<int, GameObject> _protoLookup; // protoIndex → interactivePrefab
        private Terrain _terrain;
        private TreeInstance[] _originalTreeInstances;
        private float _updateTimer;
        private const float UpdateInterval = 0.5f;

        private void Awake()
        {
            if (!InitTerrainData()) return;
            ResolvePlayerTransform();
#if UNITY_EDITOR
            // Guard against interrupted play sessions: restores terrain even if OnDestroy is skipped.
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        private bool InitTerrainData()
        {
            _terrain = Terrain.activeTerrain;
            if (_terrain == null)
            {
                Debug.LogWarning("[TerrainResourceManager] No active terrain found.");
                return false;
            }

            // Cache before any modifications so OnDestroy can fully restore.
            _originalTreeInstances = _terrain.terrainData.treeInstances;
            BuildProtoLookup();
            BuildSpotList();

            Debug.Log($"[TerrainResourceManager] Found {_spots.Count} resource spots on terrain.");
            if (_spots.Count == 0)
                Debug.LogWarning("[TerrainResourceManager] No resource spots found. " +
                    "Check that each 'Tree Prefab' in Resource Prototypes is the exact prefab used in Terrain → Paint Trees.");

            if (_terrain.treeDistance < despawnRadius * 2f)
                Debug.LogWarning($"[TerrainResourceManager] Terrain treeDistance ({_terrain.treeDistance}) is too small — " +
                    $"rocks will be culled by Unity before they reach swap range ({despawnRadius}u). " +
                    $"Fix: Select the Terrain → Inspector → Terrain Settings → Tree & Detail Objects → " +
                    $"set Tree Distance to at least {despawnRadius * 4f}.");

            return true;
        }

        private void ResolvePlayerTransform()
        {
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

        private void BuildProtoLookup()
        {
            _protoLookup = new Dictionary<int, GameObject>();
            if (resourcePrototypes == null || resourcePrototypes.Length == 0) return;

            var treeProtos = _terrain.terrainData.treePrototypes;
            foreach (var entry in resourcePrototypes)
            {
                if (entry.treePrefab == null || entry.interactivePrefab == null) continue;
                for (int i = 0; i < treeProtos.Length; i++)
                {
                    if (treeProtos[i].prefab == entry.treePrefab)
                    {
                        _protoLookup[i] = entry.interactivePrefab;
                        Debug.Log($"[TerrainResourceManager] '{entry.treePrefab.name}' → prototype index {i} → '{entry.interactivePrefab.name}'");
                        break;
                    }
                }
            }

            if (_protoLookup.Count == 0)
                Debug.LogWarning("[TerrainResourceManager] No tree prototypes matched. " +
                    "Drag the exact prefab used in Terrain → Paint Trees into each 'Tree Prefab' slot.");
            else if (_protoLookup.Count < resourcePrototypes.Length)
                Debug.LogWarning($"[TerrainResourceManager] Only {_protoLookup.Count}/{resourcePrototypes.Length} entries matched — " +
                    "check that Tree Prefab fields point to the exact prefab in Paint Trees.");
        }

        private HashSet<int> BuildProtoIndexSet()
        {
            return new HashSet<int>(_protoLookup.Keys);
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

            ApplySpawnScale(spot);

            var node = spot.activeGO.GetComponent<ResourceNode>();
            if (node != null)
                node.OnPickedUp += OnNodePickedUp;
        }

        private void ApplySpawnScale(ResourceSpot spot)
        {
            // Capture the prefab's authored scale and LODGroup size before we touch localScale.
            Vector3 authoredScale  = spot.activeGO.transform.localScale;
            var     lodGroup       = spot.activeGO.GetComponentInChildren<LODGroup>();
            float   authoredLodSz  = (lodGroup != null) ? lodGroup.size : 0f;

            // Use terrain widthScale/heightScale as a multiplier on top of the prefab's own scale.
            // This preserves the scale authored in the prefab while respecting terrain brush size variation.
            float ws = Mathf.Max(spot.originalInstance.widthScale, 1f);
            float hs = Mathf.Max(spot.originalInstance.heightScale, 1f);

            if (spot.originalInstance.widthScale < 1f)
                Debug.LogWarning($"[TerrainResourceManager] Rock at {spot.worldPos} has widthScale={spot.originalInstance.widthScale:F2} (<1). " +
                    "Fix: select the Terrain, open Paint Trees, click your rock prototype, and set Width to at least 1.");

            spot.activeGO.transform.localScale = new Vector3(
                authoredScale.x * ws, authoredScale.y * hs, authoredScale.z * ws);

            // LODGroup.size is a world-space reference diameter — scale it to match the final world size.
            if (lodGroup != null && authoredLodSz > 0f)
                lodGroup.size = authoredLodSz * authoredScale.x * ws;
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

        public List<Vector3> GetHarvestedPositions()
        {
            var result = new List<Vector3>();
            foreach (var spot in _spots)
                if (spot.harvested) result.Add(spot.worldPos);
            return result;
        }

        public void ApplyHarvestedPositions(List<Vector3> positions)
        {
            if (positions == null || positions.Count == 0) return;
            const float toleranceSq = 0.01f; // 10 cm radius
            foreach (var spot in _spots)
                foreach (var saved in positions)
                    if (Vector3.SqrMagnitude(spot.worldPos - saved) < toleranceSq)
                    {
                        spot.harvested = true;
                        // Destroy the GO if Update() already spawned it before LoadGame ran (frame 1 race).
                        if (spot.activeGO != null)
                        {
                            Destroy(spot.activeGO);
                            spot.activeGO = null;
                        }
                        break;
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
            {
                td.treeInstances = updated.ToArray();
                ForceTerrainColliderRebuild();
            }
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
            ForceTerrainColliderRebuild();
        }

        private void ForceTerrainColliderRebuild()
        {
            // Setting treeInstances is asynchronous with respect to PhysX — the terrain tree
            // capsule collider for the removed/added tree won't disappear until the terrain
            // collider syncs. Toggling the TerrainCollider forces an immediate capsule rebuild
            // so the player never walks into an invisible ghost collider after pickup.
            var tc = _terrain.GetComponent<TerrainCollider>();
            if (tc == null) return;
            tc.enabled = false;
            tc.enabled = true;
        }

        private GameObject GetPrefabForProto(int protoIndex)
        {
            _protoLookup.TryGetValue(protoIndex, out var prefab);
            return prefab;
        }
    }
}
