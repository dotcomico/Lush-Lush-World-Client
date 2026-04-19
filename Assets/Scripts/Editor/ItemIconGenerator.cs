using System.IO;
using LushWorld.Inventory;
using UnityEditor;
using UnityEngine;

namespace LushWorld.Editor
{
    public static class ItemIconGenerator
    {
        private const string IconOutputFolder = "Assets/App/Items/Icons";
        private const int IconSize = 128;

        // Instantiate far from the play area so it never affects gameplay.
        private static readonly Vector3 OffscreenPos = new Vector3(99000f, 99000f, 99000f);

        // Right-click a prefab in the Project window → generate its icon directly,
        // without needing WorldPrefab wired up in any ItemDefinition first.
        [MenuItem("Assets/Lush World/Generate Icon For This Prefab")]
        private static void GenerateIconForSelection()
        {
            var prefab = Selection.activeObject as GameObject;
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Generate Icon", "Select a prefab first.", "OK");
                return;
            }

            if (!Directory.Exists(IconOutputFolder))
                Directory.CreateDirectory(IconOutputFolder);

            string itemId = prefab.name
                .Replace("World_", "")
                .Replace("PT_", "")
                .Replace(" Variant", "")
                .Replace(" ", "_")
                .ToLower();

            string pngPath = $"{IconOutputFolder}/{itemId}.png";

            Texture2D icon = RenderPrefabIcon(prefab);
            if (icon == null)
            {
                EditorUtility.DisplayDialog("Generate Icon", "Could not render — prefab has no Renderers.", "OK");
                return;
            }

            File.WriteAllBytes(pngPath, icon.EncodeToPNG());
            Object.DestroyImmediate(icon);

            AssetDatabase.ImportAsset(pngPath);
            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            AssetDatabase.Refresh();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            int assigned = 0;
            if (sprite != null)
            {
                foreach (string defGuid in AssetDatabase.FindAssets("t:ItemDefinition"))
                {
                    string defPath = AssetDatabase.GUIDToAssetPath(defGuid);
                    var def = AssetDatabase.LoadAssetAtPath<ItemDefinition>(defPath);
                    if (def == null || def.ItemId != itemId) continue;

                    var so = new SerializedObject(def);
                    so.FindProperty("<Icon>k__BackingField").objectReferenceValue = sprite;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(def);
                    assigned++;
                }
                AssetDatabase.SaveAssets();
            }

            string result = assigned > 0
                ? $"Icon saved and assigned to ItemDefinition '{itemId}'."
                : $"Icon saved to {pngPath}\n\nNo ItemDefinition with ItemId '{itemId}' found — assign the icon manually.";

            Debug.Log($"[ItemIconGenerator] {result}");
            EditorUtility.DisplayDialog("Generate Icon", result, "OK");
        }

        [MenuItem("Assets/Lush World/Generate Icon For This Prefab", true)]
        private static bool GenerateIconForSelectionValidate()
            => Selection.activeObject is GameObject;

        [MenuItem("Lush World/Generate Item Icons")]
        public static void GenerateIcons()
        {
            if (!Directory.Exists(IconOutputFolder))
                Directory.CreateDirectory(IconOutputFolder);

            string[] guids = AssetDatabase.FindAssets("t:ItemDefinition");
            int generated = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);

                if (definition == null || definition.Icon != null || definition.WorldPrefab == null)
                    continue;

                Texture2D icon = RenderPrefabIcon(definition.WorldPrefab);
                if (icon == null)
                {
                    Debug.LogWarning($"[ItemIconGenerator] Could not render icon for '{definition.ItemId}'. Skipping.");
                    continue;
                }

                string pngPath = $"{IconOutputFolder}/{definition.ItemId}.png";
                File.WriteAllBytes(pngPath, icon.EncodeToPNG());
                Object.DestroyImmediate(icon);

                AssetDatabase.ImportAsset(pngPath);
                var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
                if (sprite != null)
                {
                    var so = new SerializedObject(definition);
                    so.FindProperty("<Icon>k__BackingField").objectReferenceValue = sprite;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(definition);
                    generated++;
                    Debug.Log($"[ItemIconGenerator] Icon generated for '{definition.ItemId}' → {pngPath}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Item Icon Generator",
                generated > 0
                    ? $"Done! Generated {generated} icon(s)."
                    : "No items needed icons (all have icons, or WorldPrefab is not set).",
                "OK");
        }

        private static Texture2D RenderPrefabIcon(GameObject prefab)
        {
            // PrefabUtility.InstantiatePrefab is required in Editor mode — Object.Instantiate
            // throws InvalidCastException on prefab assets outside play mode.
            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (go == null)
            {
                Debug.LogWarning($"[ItemIconGenerator] PrefabUtility.InstantiatePrefab returned null for '{prefab.name}'.");
                return null;
            }
            go.transform.position = OffscreenPos;
            go.transform.rotation = Quaternion.identity;

            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Object.DestroyImmediate(go);
                return null;
            }

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float dist = Mathf.Max(bounds.extents.magnitude, 0.01f);

            var camGo = new GameObject("__IconCam__");
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.fieldOfView = 30f;
            cam.cullingMask = ~0;
            cam.transform.position = bounds.center + new Vector3(dist * 0.8f, dist * 0.8f, -dist * 3f);
            cam.transform.LookAt(bounds.center);
            cam.nearClipPlane = dist * 0.01f;
            cam.farClipPlane = dist * 30f;

            var lightGo = new GameObject("__IconLight__");
            var lt = lightGo.AddComponent<Light>();
            lt.type = LightType.Directional;
            lt.intensity = 1.5f;
            lt.transform.rotation = Quaternion.Euler(45f, 45f, 0f);

            var rt = new RenderTexture(IconSize, IconSize, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(lightGo);

            return tex;
        }
    }
}
