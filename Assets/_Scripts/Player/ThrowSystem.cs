using UnityEngine;
using UnityEngine.UI;
using RunLight.Inventory;
using RunLight.UI;

namespace RunLight.Player
{
    public class ThrowSystem : MonoBehaviour
    {
        public static ThrowSystem Instance { get; private set; }
        public static bool IsAiming { get; private set; }

        [SerializeField] private float throwForce  = 12f;
        [SerializeField] private float throwArc    = 0.25f;   // 拋物弧度
        [SerializeField] private Transform cameraTransform;

        private InventoryItem _pendingItem;
        private Canvas        _crosshairCanvas;
        private Image         _crosshairDot;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            BuildCrosshair();
        }

        private void Start()
        {
            if (cameraTransform == null)
                cameraTransform = GetComponentInChildren<Camera>()?.transform;
        }

        private void Update()
        {
            if (!IsAiming) return;

            _crosshairDot.gameObject.SetActive(true);

            if (Input.GetMouseButtonDown(0))
                Throw();

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
                CancelAim();
        }

        public void StartAiming(InventoryItem item)
        {
            _pendingItem = item;
            IsAiming     = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void Throw()
        {
            if (_pendingItem == null) { CancelAim(); return; }

            // cameraTransform 最後防線
            if (cameraTransform == null) cameraTransform = Camera.main?.transform ?? transform;
            if (cameraTransform == null) { Debug.LogError("[ThrowSystem] 找不到 Camera"); CancelAim(); return; }

            InventorySystem.Instance?.Remove(_pendingItem.id);

            // 建立拋出物
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position   = cameraTransform.position + cameraTransform.forward * 1.2f;
            go.transform.localScale = Vector3.one * 0.4f;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(mr.sharedMaterial);
                mat.SetColor("_BaseColor", new Color(0.9f, 0.8f, 0.3f));
                mat.SetColor("_Color",     new Color(0.9f, 0.8f, 0.3f));
                mr.material = mat;
            }

            // 忽略和玩家碰撞體的碰撞
            var playerCol = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
            var throwCol  = go.GetComponent<Collider>();
            if (playerCol != null && throwCol != null)
                Physics.IgnoreCollision(throwCol, playerCol);

            var rb = go.AddComponent<Rigidbody>();
            var dir = (cameraTransform.forward + Vector3.up * throwArc).normalized;
            rb.linearVelocity = dir * throwForce;

            var thrown = go.AddComponent<Interaction.ThrownItem>();
            thrown.sourceItem = _pendingItem;

            CancelAim();
        }

        private void CancelAim()
        {
            IsAiming     = false;
            _pendingItem = null;
            if (_crosshairDot != null) _crosshairDot.gameObject.SetActive(false);
        }

        private void BuildCrosshair()
        {
            var cgo = new GameObject("CrosshairCanvas", typeof(Canvas), typeof(CanvasScaler));
            cgo.transform.SetParent(transform, false);
            var cv = cgo.GetComponent<Canvas>();
            cv.renderMode   = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 25;
            var sc = cgo.GetComponent<CanvasScaler>();
            sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            _crosshairCanvas = cv;

            // 外圈
            var ring = new GameObject("Ring", typeof(Image));
            ring.transform.SetParent(cgo.transform, false);
            var ri = ring.GetComponent<Image>();
            ri.color = new Color(1f, 1f, 1f, 0.6f);
            var rrt = ring.GetComponent<RectTransform>();
            rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0.5f);
            rrt.sizeDelta = new Vector2(28f, 28f);

            // 中心點
            var dot = new GameObject("Dot", typeof(Image));
            dot.transform.SetParent(cgo.transform, false);
            _crosshairDot = dot.GetComponent<Image>();
            _crosshairDot.color = new Color(1f, 1f, 1f, 0.9f);
            var drt = dot.GetComponent<RectTransform>();
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(6f, 6f);

            ring.SetActive(false);
            dot.SetActive(false);

            // 改成只顯示外圈+點（共用同一個 SetActive）
            _crosshairDot = ri;   // 統一控制外圈
            ring.SetActive(false);
        }
    }
}
