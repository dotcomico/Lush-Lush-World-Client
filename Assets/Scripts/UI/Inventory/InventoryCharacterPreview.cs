using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI.Inventory
{
    // Drives the Minecraft-style 3D character preview inside the backpack panel.
    // Creates an off-screen snail dummy + dedicated camera at runtime, renders them
    // to a RenderTexture, and feeds that texture into the [CharacterRawImage] child.
    public class InventoryCharacterPreview : MonoBehaviour
    {
        [Header("Model Prefabs")]
        [SerializeField] private GameObject bodyPrefab;
        [SerializeField] private GameObject shellPrefab;

        [Header("Render Target")]
        [SerializeField] private RenderTexture previewRT;

        [Header("Preview Settings")]
        [SerializeField] private float cameraFOV = 28f;
        [SerializeField] private float cameraDistance = 4.5f;
        [SerializeField] private float cameraHeightOffset = 0.8f;
        [SerializeField] private Vector3 dummyRotation = new Vector3(-8f, 20f, 0f);
        [SerializeField] private float autoRotateSpeed = 18f;

        private const int PreviewLayer = 9; // "InventoryPreview"
        private const float AnchorOffsetX = 9000f; // far off-scene, never seen by main camera

        private RawImage _rawImage;
        private GameObject _anchor;
        private GameObject _dummyRoot;
        private UnityEngine.Camera _previewCamera;

        private void Awake()
        {
            _rawImage = GetComponentInChildren<RawImage>();
        }

        private void Start()
        {
            if (previewRT == null || bodyPrefab == null || shellPrefab == null)
            {
                Debug.LogWarning("[InventoryCharacterPreview] Missing references — preview disabled.");
                return;
            }

            BuildPreviewScene();
            ExcludeLayerFromMainCamera();

            if (_rawImage != null)
                _rawImage.texture = previewRT;
        }

        private void OnDestroy()
        {
            if (_anchor != null)
                Destroy(_anchor);
        }

        private void Update()
        {
            if (_dummyRoot != null && autoRotateSpeed > 0f)
                _dummyRoot.transform.Rotate(Vector3.up, autoRotateSpeed * Time.deltaTime, Space.World);
        }

        private void BuildPreviewScene()
        {
            // Place anchor far off to the side — well outside any playable area
            _anchor = new GameObject("[InventoryPreviewAnchor]");
            _anchor.transform.position = new Vector3(AnchorOffsetX, 0f, 0f);
            DontDestroyOnLoad(_anchor);

            // Snail dummy root (body + shell children)
            _dummyRoot = new GameObject("[SnailDummy]");
            _dummyRoot.transform.SetParent(_anchor.transform, false);
            _dummyRoot.transform.localPosition = Vector3.zero;
            _dummyRoot.transform.localEulerAngles = dummyRotation;

            var body = Instantiate(bodyPrefab, _dummyRoot.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            SetLayerRecursive(body, PreviewLayer);

            var shell = Instantiate(shellPrefab, _dummyRoot.transform);
            shell.transform.localPosition = Vector3.zero;
            shell.transform.localRotation = Quaternion.identity;
            SetLayerRecursive(shell, PreviewLayer);

            // Dedicated preview camera
            var camGO = new GameObject("[InventoryPreviewCamera]");
            camGO.transform.SetParent(_anchor.transform, false);
            camGO.transform.localPosition = new Vector3(0f, cameraHeightOffset, cameraDistance);
            camGO.transform.LookAt(
                _anchor.transform.position + Vector3.up * (cameraHeightOffset * 0.5f));

            _previewCamera = camGO.AddComponent<UnityEngine.Camera>();
            _previewCamera.cullingMask = 1 << PreviewLayer;
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = new Color(0.07f, 0.08f, 0.10f, 0f);
            _previewCamera.targetTexture = previewRT;
            _previewCamera.fieldOfView = cameraFOV;
            _previewCamera.nearClipPlane = 0.1f;
            _previewCamera.farClipPlane = 50f;
            _previewCamera.depth = -10;
            _previewCamera.allowHDR = false;
            _previewCamera.allowMSAA = false;
        }

        // Ensure the main camera never accidentally sees InventoryPreview-layer objects.
        private void ExcludeLayerFromMainCamera()
        {
            var mainCam = UnityEngine.Camera.main;
            if (mainCam == null) return;
            mainCam.cullingMask &= ~(1 << PreviewLayer);
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }
    }
}
