using UnityEngine;
using UnityEngine.UI;
using RunLight.Core;

namespace RunLight.UI
{
    public class GameHUD : MonoBehaviour
    {
        [Header("位置（左上角）")]
        [SerializeField] private float marginX  = 20f;
        [SerializeField] private float marginY  = 20f;

        [Header("自訂圖片（選填，不填用純色）")]
        [SerializeField] private Sprite barBGSprite;
        [SerializeField] private Sprite barFillSprite;

        [Header("條的尺寸")]
        [SerializeField] private float barWidth  = 600f;
        [SerializeField] private float barHeight = 40f;

        [Header("字體大小")]
        [SerializeField] private int fontSize = 36;

        [Header("道具欄按鈕圖片（選填）")]
        [SerializeField] private Sprite inventoryButtonSprite;

        [Header("道具欄格子圖片（選填）")]
        [SerializeField] private Sprite itemSprite1;
        [SerializeField] private Sprite itemSprite2;
        [SerializeField] private Sprite itemSprite3;
        [SerializeField] private Sprite itemSprite4;
        [SerializeField] private Sprite itemSprite5;

        private Image     _brainFill;
        private Text      _brainLabel;
        private Image     _dangerOverlay;
        private GameObject _inventoryPanel;
        private Font      _font;

        private bool  _isDanger;
        private float _pulseTimer;
        private bool  _inventoryOpen;

        private static readonly Color FillColor  = new(0.15f, 0.60f, 0.90f, 1f);
        private static readonly Color BarBgColor = new(0.05f, 0.05f, 0.08f, 0.65f);

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildHUD();
        }

        private void Start()
        {
            if (PlayerStats.Instance == null) return;
            PlayerStats.Instance.OnBrainPowerChanged += UpdateBrainPower;
            UpdateBrainPower(PlayerStats.Instance.CurrentBrainPower, PlayerStats.Instance.MaxBrainPower);
        }

        private void OnDestroy()
        {
            if (PlayerStats.Instance == null) return;
            PlayerStats.Instance.OnBrainPowerChanged -= UpdateBrainPower;
        }

        private void Update()
        {
            if (_isDanger && _dangerOverlay != null)
            {
                _pulseTimer += Time.deltaTime * 2f;
                float alpha = Mathf.Lerp(0.15f, 0.45f, (Mathf.Sin(_pulseTimer) + 1f) * 0.5f);
                _dangerOverlay.color = new Color(0.7f, 0f, 0f, alpha);
            }

            // Tab 開關道具欄
            if (Input.GetKeyDown(KeyCode.Tab))
                ToggleInventory();
        }

        private void ToggleInventory()
        {
            _inventoryOpen = !_inventoryOpen;
            if (_inventoryPanel != null) _inventoryPanel.SetActive(_inventoryOpen);

            // 開道具欄時解鎖游標
            Cursor.lockState = _inventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible   = _inventoryOpen;
        }

        private void UpdateBrainPower(int current, int max)
        {
            float pct = (float)current / max;
            if (_brainFill  != null) _brainFill.fillAmount = pct;
            if (_brainLabel != null) _brainLabel.text = $"智力 {Mathf.RoundToInt(pct * 100)}%";

            // 智力低於 20% 時畫面閃紅
            _isDanger = pct <= 0.2f;
            if (_dangerOverlay != null && !_isDanger)
                _dangerOverlay.color = new Color(0f, 0f, 0f, 0f);

            // 智力越低條越紅
            if (_brainFill != null)
                _brainFill.color = pct <= 0.2f
                    ? Color.Lerp(new Color(1f, 0.2f, 0.2f, 1f), FillColor, pct / 0.2f)
                    : FillColor;
        }

        private void BuildHUD()
        {
            var canvasGo = new GameObject("GameHUDCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            // 背景條
            var bgGo = new GameObject("BarBG", typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.raycastTarget = false;
            if (barBGSprite != null) { bgImg.sprite = barBGSprite; bgImg.type = Image.Type.Sliced; bgImg.color = Color.white; }
            else bgImg.color = BarBgColor;
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin        = new Vector2(0f, 1f);
            bgRt.anchorMax        = new Vector2(0f, 1f);
            bgRt.pivot            = new Vector2(0f, 1f);
            bgRt.anchoredPosition = new Vector2(marginX, -marginY - fontSize - 6f);
            bgRt.sizeDelta        = new Vector2(barWidth, barHeight);

            // 填充
            var fillGo = new GameObject("BrainFill", typeof(Image));
            fillGo.transform.SetParent(bgGo.transform, false);
            var fill = fillGo.GetComponent<Image>();
            if (barFillSprite != null) { fill.sprite = barFillSprite; fill.color = Color.white; }
            else fill.color = FillColor;
            fill.type        = Image.Type.Filled;
            fill.fillMethod  = Image.FillMethod.Horizontal;
            fill.fillOrigin  = (int)Image.OriginHorizontal.Left;
            fill.fillAmount  = 0.5f;
            fill.raycastTarget = false;
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            _brainFill = fill;

            // 標籤
            var lblGo = new GameObject("BrainLabel", typeof(Text));
            lblGo.transform.SetParent(canvasGo.transform, false);
            var lbl = lblGo.GetComponent<Text>();
            lbl.font             = _font;
            lbl.text             = "智力 50%";
            lbl.fontSize         = fontSize;
            lbl.color            = Color.white;
            lbl.fontStyle        = FontStyle.Bold;
            lbl.alignment        = TextAnchor.LowerLeft;
            lbl.raycastTarget    = false;
            lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
            lbl.verticalOverflow   = VerticalWrapMode.Overflow;
            var lblOutline = lblGo.AddComponent<Outline>();
            lblOutline.effectColor    = new Color(0f, 0f, 0f, 0.85f);
            lblOutline.effectDistance = new Vector2(1.5f, -1.5f);
            var lblRt = lblGo.GetComponent<RectTransform>();
            lblRt.anchorMin        = new Vector2(0f, 1f);
            lblRt.anchorMax        = new Vector2(0f, 1f);
            lblRt.pivot            = new Vector2(0f, 1f);
            lblRt.anchoredPosition = new Vector2(marginX, -marginY);
            lblRt.sizeDelta        = new Vector2(200f, fontSize + 6f);
            _brainLabel = lbl;

            // 右下角道具欄按鈕
            var btnGo = new GameObject("InventoryButton", typeof(Image));
            btnGo.transform.SetParent(canvasGo.transform, false);
            var btnImg = btnGo.GetComponent<Image>();
            btnImg.raycastTarget = false;
            if (inventoryButtonSprite != null)
            {
                btnImg.sprite = inventoryButtonSprite;
                btnImg.color  = Color.white;
            }
            else
            {
                btnImg.color = new Color(0.1f, 0.1f, 0.12f, 0.80f);
            }
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin        = new Vector2(1f, 0f);
            btnRt.anchorMax        = new Vector2(1f, 0f);
            btnRt.pivot            = new Vector2(1f, 0f);
            btnRt.anchoredPosition = new Vector2(-20f, 20f);
            btnRt.sizeDelta        = new Vector2(90f, 90f);

            // 按鈕上的文字提示
            var btnLblGo = new GameObject("BtnLabel", typeof(Text));
            btnLblGo.transform.SetParent(btnGo.transform, false);
            var btnLbl = btnLblGo.GetComponent<Text>();
            btnLbl.font      = _font;
            btnLbl.text      = "道具\nTab";
            btnLbl.fontSize  = 16;
            btnLbl.horizontalOverflow = HorizontalWrapMode.Wrap;
            btnLbl.verticalOverflow   = VerticalWrapMode.Overflow;
            btnLbl.color     = Color.white;
            btnLbl.fontStyle = FontStyle.Bold;
            btnLbl.alignment = TextAnchor.MiddleCenter;
            btnLbl.raycastTarget = false;
            var btnLblOutline = btnLblGo.AddComponent<Outline>();
            btnLblOutline.effectColor    = new Color(0f, 0f, 0f, 0.8f);
            btnLblOutline.effectDistance = new Vector2(1.5f, -1.5f);
            var btnLblRt = btnLblGo.GetComponent<RectTransform>();
            btnLblRt.anchorMin = Vector2.zero;
            btnLblRt.anchorMax = Vector2.one;
            btnLblRt.offsetMin = btnLblRt.offsetMax = Vector2.zero;

            // 道具欄面板（底部橫條，預設隱藏）
            _inventoryPanel = new GameObject("InventoryPanel", typeof(Image));
            _inventoryPanel.transform.SetParent(canvasGo.transform, false);
            var panelImg = _inventoryPanel.GetComponent<Image>();
            panelImg.color        = new Color(0.05f, 0.05f, 0.08f, 0.75f);
            panelImg.raycastTarget = true;
            var panelRt = _inventoryPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot     = new Vector2(0.5f, 0f);
            panelRt.anchoredPosition = new Vector2(0f, 0f);
            panelRt.sizeDelta = new Vector2(0f, 180f);

            // 標題
            var titleGo = new GameObject("Title", typeof(Text));
            titleGo.transform.SetParent(_inventoryPanel.transform, false);
            var title = titleGo.GetComponent<Text>();
            title.font      = _font;
            title.text      = "道具欄";
            title.fontSize  = 22;
            title.color     = new Color(1f, 1f, 1f, 0.6f);
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.UpperLeft;
            title.raycastTarget = false;
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin        = new Vector2(0f, 1f);
            titleRt.anchorMax        = new Vector2(1f, 1f);
            titleRt.pivot            = new Vector2(0f, 1f);
            titleRt.anchoredPosition = new Vector2(20f, -8f);
            titleRt.sizeDelta        = new Vector2(0f, 28f);

            // 道具格子（橫排）
            float slotSize  = 80f;
            float slotGap   = 60f;
            string[] slots   = { "紙條", "回溯", "誘餌", "", "" };
            Sprite[] sprites = { itemSprite1, itemSprite2, itemSprite3, itemSprite4, itemSprite5 };
            float totalWidth = slots.Length * slotSize + (slots.Length - 1) * slotGap;
            float startX     = -totalWidth / 2f;
            for (int i = 0; i < slots.Length; i++)
            {
                float xPos = startX + i * (slotSize + slotGap);

                var slotGo = new GameObject("Slot_" + slots[i], typeof(Image));
                slotGo.transform.SetParent(_inventoryPanel.transform, false);
                var slotImg = slotGo.GetComponent<Image>();
                slotImg.raycastTarget = false;
                if (sprites[i] != null) { slotImg.sprite = sprites[i]; slotImg.color = Color.white; }
                else slotImg.color = new Color(0f, 0f, 0f, 0f);
                var slotRt = slotGo.GetComponent<RectTransform>();
                slotRt.anchorMin        = new Vector2(0.5f, 0.5f);
                slotRt.anchorMax        = new Vector2(0.5f, 0.5f);
                slotRt.pivot            = new Vector2(0f, 0.5f);
                slotRt.anchoredPosition = new Vector2(xPos, 0f);
                slotRt.sizeDelta        = new Vector2(slotSize, slotSize);

                var slotLblGo = new GameObject("SlotLabel", typeof(Text));
                slotLblGo.transform.SetParent(slotGo.transform, false);
                var slotLblTxt = slotLblGo.GetComponent<Text>();
                slotLblTxt.font      = _font;
                slotLblTxt.text      = slots[i];
                slotLblTxt.fontSize  = 16;
                slotLblTxt.color     = new Color(1f, 1f, 1f, 0.7f);
                slotLblTxt.alignment = TextAnchor.LowerCenter;
                slotLblTxt.raycastTarget = false;
                slotLblTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                var slotLblRt = slotLblGo.GetComponent<RectTransform>();
                slotLblRt.anchorMin = Vector2.zero;
                slotLblRt.anchorMax = Vector2.one;
                slotLblRt.offsetMin = new Vector2(2f, 4f);
                slotLblRt.offsetMax = new Vector2(-2f, -4f);
            }

            _inventoryPanel.SetActive(false);

            // 危險覆蓋層
            var overlayGo = new GameObject("DangerOverlay", typeof(Image));
            overlayGo.transform.SetParent(canvasGo.transform, false);
            _dangerOverlay = overlayGo.GetComponent<Image>();
            _dangerOverlay.color        = new Color(0f, 0f, 0f, 0f);
            _dangerOverlay.raycastTarget = false;
            var oRt = overlayGo.GetComponent<RectTransform>();
            oRt.anchorMin = Vector2.zero;
            oRt.anchorMax = Vector2.one;
            oRt.offsetMin = oRt.offsetMax = Vector2.zero;
        }
    }
}
