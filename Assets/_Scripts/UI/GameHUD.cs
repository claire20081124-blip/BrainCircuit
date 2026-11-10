using UnityEngine;
using UnityEngine.UI;
using RunLight.Core;
using RunLight.Interaction;

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

        private Image         _brainFill;
        private RectTransform _fillClipRt;
        private Text          _brainLabel;
        private Image  _dangerOverlay;
        private Font   _font;

        private bool  _isDanger;
        private float _pulseTimer;

        private static readonly Color FillColor  = new(0.212f, 0.349f, 0.502f, 1f);
        private static readonly Color BarBgColor = new(0.05f, 0.05f, 0.08f, 0.65f);

        private static GameHUD _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildHUD();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.OnBrainPowerChanged -= UpdateBrainPower;
        }

        private void Start()
        {
            if (PlayerStats.Instance == null) return;
            PlayerStats.Instance.OnBrainPowerChanged += UpdateBrainPower;
            UpdateBrainPower(PlayerStats.Instance.CurrentBrainPower, PlayerStats.Instance.MaxBrainPower);
        }

        private void OnGUI() => InteractionHint.Draw();

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
            float pct = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            if (_fillClipRt != null)
                _fillClipRt.sizeDelta = new Vector2(barWidth * pct, _fillClipRt.sizeDelta.y);
            else if (_brainFill != null)
                _brainFill.fillAmount = pct;
            if (_brainLabel != null) _brainLabel.text = $"{Mathf.RoundToInt(pct * 100)}%";

            _isDanger = current < 10;
            if (_dangerOverlay != null && !_isDanger)
                _dangerOverlay.color = new Color(0f, 0f, 0f, 0f);

            if (_brainFill != null && barFillSprite == null)
                _brainFill.color = _isDanger ? new Color(1f, 0.2f, 0.2f, 1f) : FillColor;
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

            // 容器
            var barContainer = new GameObject("BarContainer", typeof(RectTransform));
            barContainer.transform.SetParent(canvasGo.transform, false);
            var containerRt = barContainer.GetComponent<RectTransform>();
            containerRt.anchorMin        = new Vector2(0f, 1f);
            containerRt.anchorMax        = new Vector2(0f, 1f);
            containerRt.pivot            = new Vector2(0f, 1f);
            containerRt.anchoredPosition = new Vector2(marginX, -marginY - fontSize - 6f);
            containerRt.sizeDelta        = new Vector2(barWidth, barHeight);

            // 裁切容器（RectMask2D 控制填充寬度）
            var clipGo = new GameObject("FillClip", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D));
            clipGo.transform.SetParent(barContainer.transform, false);
            var clipRt = clipGo.GetComponent<RectTransform>();
            clipRt.anchorMin = new Vector2(0f, 0f);
            clipRt.anchorMax = new Vector2(0f, 1f);
            clipRt.pivot     = new Vector2(0f, 0.5f);
            clipRt.offsetMin = clipRt.offsetMax = Vector2.zero;
            clipRt.sizeDelta = new Vector2(barWidth * 0.5f, 0f);
            _fillClipRt = clipRt;

            // 填充圖（永遠全寬，由裁切容器決定顯示多少）
            var fillGo = new GameObject("BrainFill", typeof(Image));
            fillGo.transform.SetParent(clipGo.transform, false);
            var fill = fillGo.GetComponent<Image>();
            if (barFillSprite != null)
            {
                fill.sprite = barFillSprite;
                fill.type   = Image.Type.Simple;
                fill.color  = Color.white;
                fill.preserveAspect = false;
            }
            else
            {
                fill.color      = FillColor;
                fill.type       = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                fill.fillAmount = 1f;
            }
            fill.raycastTarget = false;
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot     = new Vector2(0f, 0.5f);
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            fillRt.sizeDelta = new Vector2(barWidth, 0f);
            _brainFill = fill;

            // 外框（後加 = 在上層，蓋住填充邊緣）
            var bgGo = new GameObject("BarBG", typeof(Image));
            bgGo.transform.SetParent(barContainer.transform, false);
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.raycastTarget = false;
            if (barBGSprite != null)
            {
                bgImg.sprite = barBGSprite;
                bgImg.type   = Image.Type.Sliced;
                bgImg.color  = Color.white;
                bgImg.fillCenter = false;
            }
            else bgImg.color = BarBgColor;
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            // 標籤
            var lblGo = new GameObject("BrainLabel", typeof(Text));
            lblGo.transform.SetParent(canvasGo.transform, false);
            var lbl = lblGo.GetComponent<Text>();
            lbl.font             = _font;
            lbl.text             = "50%";
            lbl.fontSize         = fontSize;
            lbl.color            = Color.white;
            lbl.fontStyle        = FontStyle.Bold;
            lbl.alignment        = TextAnchor.MiddleLeft;
            lbl.raycastTarget    = false;
            lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
            lbl.verticalOverflow   = VerticalWrapMode.Overflow;
            var lblOutline = lblGo.AddComponent<Outline>();
            lblOutline.effectColor    = new Color(0f, 0f, 0f, 0.85f);
            lblOutline.effectDistance = new Vector2(1.5f, -1.5f);
            var lblRt = lblGo.GetComponent<RectTransform>();
            lblRt.anchorMin        = new Vector2(0f, 1f);
            lblRt.anchorMax        = new Vector2(0f, 1f);
            lblRt.pivot            = new Vector2(0f, 0.5f);
            lblRt.anchoredPosition = new Vector2(marginX + barWidth + 8f,
                                                 -(marginY + fontSize + 6f + barHeight * 0.5f));
            lblRt.sizeDelta        = new Vector2(120f, fontSize + 6f);
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
