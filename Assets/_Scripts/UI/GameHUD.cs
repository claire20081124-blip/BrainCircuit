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
        [SerializeField] private float barWidth  = 900f;
        [SerializeField] private float barHeight = 40f;

        [Header("字體大小")]
        [SerializeField] private int fontSize = 36;

        private Image  _brainFill;
        private Text   _brainLabel;
        private Image  _dangerOverlay;
        private Font   _font;

        private bool  _isDanger;
        private float _pulseTimer;

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
        }

        private void UpdateBrainPower(int current, int max)
        {
            float pct = (float)current / max;
            if (_brainFill  != null) _brainFill.fillAmount = pct;
            if (_brainLabel != null) _brainLabel.text = $"智力 {Mathf.RoundToInt(pct * 100)}%";

            _isDanger = pct <= 0.2f;
            if (_dangerOverlay != null && !_isDanger)
                _dangerOverlay.color = new Color(0f, 0f, 0f, 0f);

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
