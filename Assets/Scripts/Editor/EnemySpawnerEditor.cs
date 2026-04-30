using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LushWorld.Enemies
{
    [CustomEditor(typeof(EnemySpawner))]
    public class EnemySpawnerEditor : UnityEditor.Editor
    {
        private int   _count            = 10;
        private float _radius           = 50f;
        private int   _groundLayerIndex = 0; // Default layer

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Scatter Tool", EditorStyles.boldLabel);
            _count            = EditorGUILayout.IntField("Point Count", _count);
            _radius           = EditorGUILayout.FloatField("Radius (m)", _radius);
            _groundLayerIndex = EditorGUILayout.LayerField("Ground Layer", _groundLayerIndex);

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Scatter Spawn Points", GUILayout.Height(32)))
                Scatter();

            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Clear Spawn Points"))
                Clear();
            GUI.color = Color.white;
        }

        // Draws a green disc in the Scene view showing the scatter area.
        private void OnSceneGUI()
        {
            var spawner = (EnemySpawner)target;
            Handles.color = new Color(0f, 1f, 0.3f, 0.12f);
            Handles.DrawSolidDisc(spawner.transform.position, Vector3.up, _radius);
            Handles.color = new Color(0f, 1f, 0.3f, 0.8f);
            Handles.DrawWireDisc(spawner.transform.position, Vector3.up, _radius);
        }

        private void Scatter()
        {
            var spawner   = (EnemySpawner)target;
            int layerMask = 1 << _groundLayerIndex;
            Vector3 center = spawner.transform.position;

            // Remove auto-generated children from any previous scatter.
            var toDestroy = new List<GameObject>();
            foreach (Transform child in spawner.transform)
                if (child.name.StartsWith("SpawnPoint_"))
                    toDestroy.Add(child.gameObject);
            foreach (var go in toDestroy)
                Undo.DestroyObjectImmediate(go);

            var created = new List<Transform>();
            int attempts = _count * 10; // extra attempts for points that miss the terrain

            for (int i = 0; i < attempts && created.Count < _count; i++)
            {
                Vector2 rnd    = Random.insideUnitCircle * _radius;
                Vector3 origin = new Vector3(center.x + rnd.x, center.y + 500f, center.z + rnd.y);

                if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 600f, layerMask))
                    continue;

                var go = new GameObject($"SpawnPoint_{created.Count:00}");
                Undo.RegisterCreatedObjectUndo(go, "Scatter Spawn Points");
                go.transform.SetParent(spawner.transform, worldPositionStays: true);
                go.transform.position = hit.point;
                created.Add(go.transform);
            }

            // Wire the new transforms into the _spawnPoints serialized field.
            var prop = serializedObject.FindProperty("_spawnPoints");
            prop.arraySize = created.Count;
            for (int i = 0; i < created.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = created[i];
            serializedObject.ApplyModifiedProperties();

            int missed = _count - created.Count;
            string msg = $"[EnemySpawner] Scattered {created.Count}/{_count} spawn points within {_radius}m of '{spawner.name}'.";
            if (missed > 0) msg += $" {missed} points missed terrain — try a smaller radius or check Ground Layer.";
            Debug.Log(msg, spawner);
        }

        private void Clear()
        {
            var toDestroy = new List<GameObject>();
            foreach (Transform child in ((EnemySpawner)target).transform)
                if (child.name.StartsWith("SpawnPoint_"))
                    toDestroy.Add(child.gameObject);
            foreach (var go in toDestroy)
                Undo.DestroyObjectImmediate(go);

            var prop = serializedObject.FindProperty("_spawnPoints");
            prop.ClearArray();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
