using UnityEditor;
using UnityEngine;

namespace LushWorld.Editor
{
    /// <summary>
    /// Extracts the snail visual (body_1 + shell_1) from PlayerRig overrides into a proper
    /// SnailBody.prefab, then nests it inside PlayerCapsule.prefab.
    ///
    /// Run once via Lush World > Setup > Extract SnailBody Prefab.
    /// </summary>
    public static class SnailBodyExtractor
    {
        const string PlayerRigPath       = "Assets/App/Prefabs/PlayerRig.prefab";
        const string PlayerCapsulePath   = "Assets/StarterAssets/FirstPersonController/Prefabs/PlayerCapsule.prefab";
        const string SnailBodyOutputPath = "Assets/App/Prefabs/Player/SnailBody.prefab";
        const string BodyModelPath       = "Assets/App/Models/Snail/Bodies/body_1.glb";
        const string ShellModelPath      = "Assets/App/Models/Snail/Shells/shell_1.glb";

        [MenuItem("Lush World/Setup/Extract SnailBody Prefab")]
        public static void Extract()
        {
            if (!AssetDatabase.IsValidFolder("Assets/App/Prefabs/Player"))
                AssetDatabase.CreateFolder("Assets/App/Prefabs", "Player");

            // 1 — build SnailBody.prefab from model assets
            var snailBodyPrefab = CreateSnailBodyPrefab();
            if (snailBodyPrefab == null) return;

            // 2 — nest SnailBody inside PlayerCapsule.prefab
            AddSnailBodyToPlayerCapsule(snailBodyPrefab);

            // Force Unity to recognise the saved PlayerCapsule before opening PlayerRig
            AssetDatabase.ImportAsset(PlayerCapsulePath, ImportAssetOptions.ForceUpdate);

            // 3 — remove old shell_1 override, disable capsule mesh, rewire SlideController
            CleanUpPlayerRigOverrides();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SnailBodyExtractor] Done. Open PlayerRig → PlayerCapsule to verify SnailBody (blue prefab icon).");
        }

        // ── Step 1 ────────────────────────────────────────────────────────────────

        static GameObject CreateSnailBodyPrefab()
        {
            var bodyModel  = AssetDatabase.LoadAssetAtPath<GameObject>(BodyModelPath);
            var shellModel = AssetDatabase.LoadAssetAtPath<GameObject>(ShellModelPath);

            if (!bodyModel)  { Debug.LogError("[SnailBodyExtractor] body_1.glb not found at "  + BodyModelPath);  return null; }
            if (!shellModel) { Debug.LogError("[SnailBodyExtractor] shell_1.glb not found at " + ShellModelPath); return null; }

            var root = new GameObject("SnailBody");

            // body_1 — instantiated from the .glb model asset
            var body = (GameObject)PrefabUtility.InstantiatePrefab(bodyModel);
            body.name = "body_1";
            body.transform.SetParent(root.transform, worldPositionStays: false);

            // shell_1 — instantiated with transform values preserved from the old PlayerRig overrides
            var shell = (GameObject)PrefabUtility.InstantiatePrefab(shellModel);
            shell.name = "shell_1";
            shell.transform.SetParent(root.transform, worldPositionStays: false);
            shell.transform.localPosition = new Vector3(-0.006f, 0.178f, -0.159f);
            shell.transform.localRotation = new Quaternion(0.007548916f, 0.7167423f, 0.0065670786f, 0.6972664f);
            shell.transform.localScale    = new Vector3(0.8f, 0.8f, 1.2f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, SnailBodyOutputPath, out bool ok);
            Object.DestroyImmediate(root);

            if (!ok || !prefab)
            {
                Debug.LogError("[SnailBodyExtractor] SaveAsPrefabAsset failed — check Console for details.");
                return null;
            }

            Debug.Log("[SnailBodyExtractor] Step 1 — SnailBody.prefab created at " + SnailBodyOutputPath);
            return prefab;
        }

        // ── Step 2 ────────────────────────────────────────────────────────────────

        static void AddSnailBodyToPlayerCapsule(GameObject snailBodyPrefab)
        {
            using var scope = new PrefabUtility.EditPrefabContentsScope(PlayerCapsulePath);
            var root = scope.prefabContentsRoot;

            if (root.transform.Find("SnailBody") != null)
            {
                Debug.Log("[SnailBodyExtractor] Step 2 skipped — SnailBody already present in PlayerCapsule.prefab.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(snailBodyPrefab, root.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale    = Vector3.one;

            Debug.Log("[SnailBodyExtractor] Step 2 — SnailBody nested inside PlayerCapsule.prefab.");
        }

        // ── Step 3 ────────────────────────────────────────────────────────────────

        static void CleanUpPlayerRigOverrides()
        {
            using var scope = new PrefabUtility.EditPrefabContentsScope(PlayerRigPath);
            var root = scope.prefabContentsRoot;

            // The Capsule GO was renamed to "Snail" via override — find by either name
            Transform capsuleT = root.transform.Find("PlayerCapsule/Snail")
                              ?? root.transform.Find("PlayerCapsule/Capsule");

            if (capsuleT == null)
            {
                Debug.LogWarning("[SnailBodyExtractor] Step 3 — Could not find Snail/Capsule GO in PlayerRig. Skipping override cleanup.");
                return;
            }

            var capsuleGO = capsuleT.gameObject;

            // 3a — Remove the shell_1 AddedGameObject override
            var shellT = capsuleT.Find("shell_1");
            if (shellT != null)
            {
                Object.DestroyImmediate(shellT.gameObject);
                Debug.Log("[SnailBodyExtractor] Step 3a — shell_1 override removed.");
            }

            // 3b — Revert the name override so Capsule is no longer called "Snail"
            //      Setting the name to the source value removes the override when the scope saves.
            capsuleGO.name = "Capsule";

            // 3c — Disable the Capsule MeshRenderer so only SnailBody is visible.
            //      The original PlayerCapsule.prefab has MeshRenderer enabled; setting it to false
            //      becomes a PlayerRig-level override (the physics Capsule stays active).
            var mr = capsuleGO.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.enabled = false;

            // 3d — Rewire SlideController._visualModel → SnailBody
            //      After Step 2, SnailBody is part of PlayerCapsule.prefab and therefore visible
            //      here inside the PlayerRig scope.
            var playerCapsuleT = root.transform.Find("PlayerCapsule");
            if (playerCapsuleT != null)
            {
                var snailBodyT = playerCapsuleT.Find("SnailBody");
                if (snailBodyT != null)
                {
                    var slideCtrl = playerCapsuleT.GetComponent("SlideController") as MonoBehaviour;
                    if (slideCtrl != null)
                    {
                        var so = new SerializedObject(slideCtrl);
                        so.FindProperty("_visualModel").objectReferenceValue = snailBodyT;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        Debug.Log("[SnailBodyExtractor] Step 3d — SlideController._visualModel → SnailBody.");
                    }
                    else
                    {
                        Debug.LogWarning("[SnailBodyExtractor] Step 3d — SlideController not found on PlayerCapsule. Assign _visualModel to SnailBody manually.");
                    }
                }
                else
                {
                    Debug.LogWarning("[SnailBodyExtractor] Step 3d — SnailBody not visible in PlayerRig scope yet. Assign SlideController._visualModel to SnailBody manually.");
                }
            }

            Debug.Log("[SnailBodyExtractor] Step 3 — PlayerRig.prefab cleanup complete.");
        }
    }
}
