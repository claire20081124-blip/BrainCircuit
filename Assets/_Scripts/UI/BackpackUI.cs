using UnityEngine;
using UnityEngine.UI;
using RunLight.Player;
using RunLight.Inventory;

namespace RunLight.UI
{
    public class BackpackUI : MonoBehaviour
    {
        public static BackpackUI Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        [SerializeField] private int   columns      = 4;
        [SerializeField] private float slotSize     = 120f;
        [SerializeField] private float slotPadding  = 12f;

        private CanvasGroup           _bg;
        private Transform             _grid;
        private GameObject            _detailPanel;
        private Text                  _detailName;
        private Text                  _detailDesc;
        private GameObject            _inspectPanel;
        private RawImage              _inspectRaw;
        private Text                  _inspectName;
        private Camera                _inspectCam;
        private RenderTexture         _inspectRT;
        private Transform             _inspectRoot;
        private GameObject            _inspectModel;
        private bool                  _isDragging;
        private Vector2               _lastMouse;
        private float                 _inspectYaw;
        private float                 _inspectPitch;
        private FirstPersonController _fpc;
        private Font                  _font;

        private static readonly Color BgColor      = new(0.05f, 0.07f, 0.12f, 0.92f);
        private static readonly Color SlotEmpty    = new(0.12f, 0.15f, 0.20f, 0.80f);
        private static readonly Color SlotFilled   = new(0.18f, 0.22f, 0.32f, 0.95f);
        private static readonly Color DetailBg     = new(0.08f, 0.10f, 0.16f, 0.97f);
        private static readonly Color AccentColor  = new(0.55f, 0.80f, 1.00f, 0.85f);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
        }

        private void Start()
        {
            _fpc = FindObjectOfType<FirstPersonController>();
            if (InventorySystem.Instance != null)
                InventorySystem.Instance.OnChanged += RefreshGrid;

            // 主相機排除 layer 31（留給 3D 查看用）
            if (Camera.main != null)
                Camera.main.cullingMask &= ~(1 << 31);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (IsOpen) Close();
                else        Open();
            }

            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                // 先關查看覆蓋層，再關背包
                if (_inspectPanel != null && _inspectPanel.activeSelf)
                    CloseInspect();
                else
                    Close();
            }

            // 3D 查看旋轉（拖曳左鍵）
            if (_inspectPanel != null && _inspectPanel.activeSelf && _inspectModel != null)
            {
                if (Input.GetMouseButtonDown(0)) { _isDragging = true; _lastMouse = Input.mousePosition; }
                if (Input.GetMouseButtonUp(0))   _isDragging = false;
                if (_isDragging)
                {
                    var delta  = (Vector2)Input.mousePosition - _lastMouse;
                    _lastMouse = Input.mousePosition;
                    _inspectYaw   += delta.x * 0.5f;
                    _inspectPitch -= delta.y * 0.5f;
                    _inspectPitch  = Mathf.Clamp(_inspectPitch, -75f, 75f);
                    _inspectModel.transform.localRotation =
                        Quaternion.Euler(_inspectPitch, _inspectYaw, 0f);
                }
            }
        }

        private void Open()
        {
            if (BrainQAUI.IsOpen) return;
            if (PauseMenuUI.IsPaused) return;
            if (CaughtUI.Instance != null && CaughtUI.Instance.IsActive) return;

            IsOpen = true;
            _bg.gameObject.SetActive(true);
            RefreshGrid();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            if (_fpc != null) _fpc.MovementLocked = true;
        }

        private void Close()
        {
            IsOpen = false;
            _bg.gameObject.SetActive(false);
            HideDetail();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            if (_fpc != null) _fpc.MovementLocked = false;
        }

        private void RefreshGrid()
        {
            if (_grid == null) return;

            // 清空舊格子
            foreach (Transform child in _grid)
                Destroy(child.gameObject);

            var items = InventorySystem.Instance?.Items;
            int totalSlots = columns * 3;   // 4×3 = 12 格

            for (int i = 0; i < totalSlots; i++)
            {
                var item = (items != null && i < items.Count) ? items[i] : null;
                CreateSlot(i, item);
            }
        }

        private void CreateSlot(int index, InventoryItem item)
        {
            var slotGo = new GameObject("Slot_" + index, typeof(Image), typeof(Button));
            slotGo.transform.SetParent(_grid, false);
            var img = slotGo.GetComponent<Image>();
            img.color = item != null ? SlotFilled : SlotEmpty;

            var rt = slotGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(slotSize, slotSize);

            if (item != null)
            {
                // 圖示（有 Sprite 用圖，否則色塊）
                var iconGo = new GameObject("Icon", typeof(Image));
                iconGo.transform.SetParent(slotGo.transform, false);
                var iconImg = iconGo.GetComponent<Image>();
                if (item.icon != null) { iconImg.sprite = item.icon; iconImg.preserveAspect = true; }
                else                  { iconImg.color = item.placeholderColor; }
                var irt = iconGo.GetComponent<RectTransform>();
                irt.anchorMin = new Vector2(0.1f, 0.3f); irt.anchorMax = new Vector2(0.9f, 0.95f);
                irt.offsetMin = irt.offsetMax = Vector2.zero;

                // 名稱標籤
                var lblGo = new GameObject("Label", typeof(Text));
                lblGo.transform.SetParent(slotGo.transform, false);
                var lbl = lblGo.GetComponent<Text>();
                lbl.font = _font; lbl.text = item.displayName;
                lbl.fontSize = 14; lbl.color = Color.white;
                lbl.alignment = TextAnchor.MiddleCenter;
                lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
                lbl.raycastTarget = false;
                var lrt = lblGo.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(1f, 0.32f);
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;

                // 點擊顯示詳情
                var captured = item;
                slotGo.GetComponent<Button>().onClick.AddListener(() => ShowDetail(captured));
            }
        }

        private InventoryItem _selectedItem;

        private void ShowDetail(InventoryItem item)
        {
            if (_detailPanel == null) return;
            _selectedItem    = item;
            _detailPanel.SetActive(true);
            _detailName.text = item.displayName;
            _detailDesc.text = item.description;
        }

        private void ThrowSelected()
        {
            if (_selectedItem == null) return;
            Close();
            Player.ThrowSystem.Instance?.StartAiming(_selectedItem);
        }

        private void InspectSelected()
        {
            if (_selectedItem == null || _inspectPanel == null) return;

            // 清掉上一個模型
            if (_inspectModel != null) { Destroy(_inspectModel); _inspectModel = null; }
            _inspectYaw = 0f; _inspectPitch = 0f; _isDragging = false;

            if (_selectedItem.inspectPrefab != null)
            {
                _inspectModel = Instantiate(_selectedItem.inspectPrefab, _inspectRoot);
            }
            else
            {
                // 色塊球 placeholder
                _inspectModel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _inspectModel.transform.SetParent(_inspectRoot, false);
                var col = _inspectModel.GetComponent<Collider>();
                if (col != null) Destroy(col);
                var mr = _inspectModel.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var mat = new Material(mr.sharedMaterial);
                    mat.SetColor("_BaseColor", _selectedItem.placeholderColor);
                    mat.SetColor("_Color",     _selectedItem.placeholderColor);
                    mr.material = mat;
                }
            }

            _inspectModel.transform.localPosition = Vector3.zero;
            _inspectModel.transform.localRotation = Quaternion.identity;
            _inspectModel.transform.localScale    = Vector3.one;

            // layer 31 so only the inspect camera sees it
            SetLayerRecursive(_inspectModel, 31);

            if (_inspectCam != null) _inspectCam.enabled = true;
            _inspectName.text = _selectedItem.displayName;
            _inspectPanel.SetActive(true);
        }

        private void CloseInspect()
        {
            if (_inspectModel != null) { Destroy(_inspectModel); _inspectModel = null; }
            if (_inspectCam  != null) _inspectCam.enabled = false;
            if (_inspectPanel != null) _inspectPanel.SetActive(false);
            _isDragging = false;
        }

        private void HideDetail()
        {
            if (_detailPanel != null) _detailPanel.SetActive(false);
            CloseInspect();
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("BackpackCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            // 背景遮罩
            var bgGo = new GameObject("BG", typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            bgGo.GetComponent<Image>().color = BgColor;
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            _bg = bgGo.AddComponent<CanvasGroup>();

            // 關閉按鈕（右上角 ✕）
            MakeCloseButton(bgGo.transform);

            // 標題
            var title = MakeTxt("Title", bgGo.transform, "背包", 38, AccentColor, FontStyle.Bold);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -40f);
            trt.sizeDelta = new Vector2(0f, 50f);
            title.alignment = TextAnchor.MiddleCenter;

            // 格子容器
            var gridGo = new GameObject("Grid", typeof(GridLayoutGroup));
            gridGo.transform.SetParent(bgGo.transform, false);
            var grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.cellSize        = new Vector2(slotSize, slotSize);
            grid.spacing         = new Vector2(slotPadding, slotPadding);
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment  = TextAnchor.MiddleCenter;
            var grt = gridGo.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 0.5f); grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.anchoredPosition = new Vector2(0f, -20f);
            float gridW = columns * (slotSize + slotPadding);
            float gridH = 3f    * (slotSize + slotPadding);
            grt.sizeDelta = new Vector2(gridW, gridH);
            _grid = gridGo.transform;

            // 3D 查看用 Camera + RT
            CreateInspectCamera();

            // 詳情面板（右側）+ 查看覆蓋層（直接掛在 canvas 上）
            BuildDetailPanel(bgGo.transform, canvasGo.transform);

            bgGo.SetActive(false);
        }

        private void BuildDetailPanel(Transform parent, Transform canvasRoot)
        {
            _detailPanel = new GameObject("Detail", typeof(Image));
            _detailPanel.transform.SetParent(parent, false);
            _detailPanel.GetComponent<Image>().color = DetailBg;
            var drt = _detailPanel.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.62f, 0.25f); drt.anchorMax = new Vector2(0.90f, 0.75f);
            drt.offsetMin = drt.offsetMax = Vector2.zero;

            _detailName = MakeTxt("DetailName", _detailPanel.transform, "", 28,
                Color.white, FontStyle.Bold);
            var nrt = _detailName.rectTransform;
            nrt.anchorMin = new Vector2(0f, 0.65f); nrt.anchorMax = Vector2.one;
            nrt.offsetMin = new Vector2(16f, 0f); nrt.offsetMax = new Vector2(-16f, -16f);
            _detailName.alignment = TextAnchor.UpperLeft;
            _detailName.horizontalOverflow = HorizontalWrapMode.Wrap;

            _detailDesc = MakeTxt("DetailDesc", _detailPanel.transform, "", 20,
                new Color(1f, 1f, 1f, 0.75f), FontStyle.Normal);
            var drt2 = _detailDesc.rectTransform;
            drt2.anchorMin = new Vector2(0f, 0.25f); drt2.anchorMax = new Vector2(1f, 0.65f);
            drt2.offsetMin = new Vector2(16f, 0f); drt2.offsetMax = new Vector2(-16f, 0f);
            _detailDesc.alignment = TextAnchor.UpperLeft;
            _detailDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailDesc.verticalOverflow   = VerticalWrapMode.Overflow;

            // 按鈕列：查看（左）| 丟出（右）
            MakeDetailButton(_detailPanel.transform,
                "InspectBtn", "查看",
                new Vector2(0f, 0f), new Vector2(0.48f, 0.22f),
                new Color(0.1f, 0.45f, 0.75f, 0.9f),
                InspectSelected);

            MakeDetailButton(_detailPanel.transform,
                "ThrowBtn", "丟出",
                new Vector2(0.52f, 0f), new Vector2(1f, 0.22f),
                new Color(0.7f, 0.45f, 0.1f, 0.9f),
                ThrowSelected);

            _detailPanel.SetActive(false);

            // 查看覆蓋層掛在 canvas 最上層（不受 bgGo.SetActive 影響）
            BuildInspectPanel(canvasRoot);
        }

        private void MakeDetailButton(Transform parent, string goName, string label,
            Vector2 anchorMin, Vector2 anchorMax, Color bgColor, UnityEngine.Events.UnityAction action)
        {
            var btn = new GameObject(goName, typeof(Image), typeof(Button));
            btn.transform.SetParent(parent, false);
            btn.GetComponent<Image>().color = bgColor;
            btn.GetComponent<Button>().onClick.AddListener(action);
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(10f, 8f); rt.offsetMax = new Vector2(-10f, -8f);

            var lbl = MakeTxt(label + "Lbl", btn.transform, label, 22, Color.white, FontStyle.Bold);
            lbl.rectTransform.anchorMin = Vector2.zero;
            lbl.rectTransform.anchorMax = Vector2.one;
            lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.raycastTarget = false;
        }

        private void CreateInspectCamera()
        {
            // 在世界 y=-500 建一個與主場景完全隔離的小舞台
            // 主相機預設 far clip = 1000，-500y 依然在範圍內
            // 所以用 cullingMask 排除：inspect camera 只看 layer 31（通常為空）
            // 而主相機不會主動包含 layer 31

            _inspectRT = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            _inspectRT.Create();

            var rootGo = new GameObject("InspectRoot");
            rootGo.transform.position = new Vector3(5000f, 0f, 0f);
            _inspectRoot = rootGo.transform;

            var camGo = new GameObject("InspectCamera");
            camGo.transform.position = new Vector3(5000f, 0f, -3f);
            camGo.transform.LookAt(_inspectRoot.position);

            _inspectCam = camGo.AddComponent<Camera>();
            _inspectCam.clearFlags      = CameraClearFlags.SolidColor;
            _inspectCam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            _inspectCam.nearClipPlane   = 0.1f;
            _inspectCam.farClipPlane    = 20f;
            _inspectCam.fieldOfView     = 40f;
            _inspectCam.cullingMask     = 1 << 31;   // layer 31 only
            _inspectCam.targetTexture   = _inspectRT;
            _inspectCam.enabled         = false;
        }

        private void BuildInspectPanel(Transform canvasTransform)
        {
            _inspectPanel = new GameObject("InspectPanel", typeof(Image));
            _inspectPanel.transform.SetParent(canvasTransform, false);
            _inspectPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.90f);
            var prt = _inspectPanel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = prt.offsetMax = Vector2.zero;

            // 關閉按鈕（右上角）
            var closeBtn = new GameObject("Close", typeof(Image), typeof(Button));
            closeBtn.transform.SetParent(_inspectPanel.transform, false);
            closeBtn.GetComponent<Image>().color = new Color(0.35f, 0.1f, 0.1f, 0.9f);
            closeBtn.GetComponent<Button>().onClick.AddListener(CloseInspect);
            var crt = closeBtn.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot     = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-30f, -30f);
            crt.sizeDelta = new Vector2(60f, 60f);
            var cLbl = MakeTxt("X", closeBtn.transform, "✕", 26, Color.white, FontStyle.Bold);
            cLbl.rectTransform.anchorMin = Vector2.zero;
            cLbl.rectTransform.anchorMax = Vector2.one;
            cLbl.rectTransform.offsetMin = cLbl.rectTransform.offsetMax = Vector2.zero;
            cLbl.alignment = TextAnchor.MiddleCenter;

            // 提示文字（左下）
            var hint = MakeTxt("Hint", _inspectPanel.transform, "拖曳滑鼠旋轉", 18,
                new Color(1f, 1f, 1f, 0.45f), FontStyle.Normal);
            var hrt = hint.rectTransform;
            hrt.anchorMin = new Vector2(0f, 0f); hrt.anchorMax = new Vector2(0.5f, 0.12f);
            hrt.offsetMin = new Vector2(20f, 10f); hrt.offsetMax = Vector2.zero;
            hint.alignment = TextAnchor.LowerLeft;

            // RenderTexture 顯示區（中央）
            var rawGo = new GameObject("InspectRaw", typeof(RawImage));
            rawGo.transform.SetParent(_inspectPanel.transform, false);
            _inspectRaw = rawGo.GetComponent<RawImage>();
            _inspectRaw.texture = _inspectRT;
            var irt = rawGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.15f, 0.16f); irt.anchorMax = new Vector2(0.85f, 0.88f);
            irt.offsetMin = irt.offsetMax = Vector2.zero;

            // 名稱（圖下方）
            _inspectName = MakeTxt("InspectName", _inspectPanel.transform, "", 30,
                Color.white, FontStyle.Bold);
            var nrt = _inspectName.rectTransform;
            nrt.anchorMin = new Vector2(0.1f, 0.05f); nrt.anchorMax = new Vector2(0.9f, 0.16f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;
            _inspectName.alignment = TextAnchor.MiddleCenter;
            _inspectName.horizontalOverflow = HorizontalWrapMode.Wrap;

            _inspectPanel.SetActive(false);
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        private void MakeCloseButton(Transform parent)
        {
            var go = new GameObject("CloseBtn", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.3f, 0.1f, 0.1f, 0.8f);
            go.GetComponent<Button>().onClick.AddListener(Close);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -20f);
            rt.sizeDelta = new Vector2(50f, 50f);

            var txt = MakeTxt("X", go.transform, "✕", 24, Color.white, FontStyle.Bold);
            var trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            txt.alignment = TextAnchor.MiddleCenter;
        }

        private Text MakeTxt(string name, Transform parent, string content,
            int size, Color color, FontStyle style)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = _font; t.text = content;
            t.fontSize = size; t.color = color; t.fontStyle = style;
            t.raycastTarget = false;
            return t;
        }
    }
}
