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
            // Instantiate temporarily in the active scene at an offscreen position.
            // Object.Instantiate is used (not PrefabUtility) to handle variant prefabs reliably.
            var go = Object.Instantiate(prefab, OffscreenPos, Quaternion.identity);

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
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
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
