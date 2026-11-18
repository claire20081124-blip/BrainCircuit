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
        [SerializeField] private Sprite[] warnSprites; // 三張警告文字圖片（選填）

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

        private bool      _isDanger;
        private float     _pulseTimer;
        private float     _dangerCycleTimer;
        private Coroutine _dangerImageRoutine;

        // ---- 低智力警告（< 60）----
        private bool       _isWarn;
        private float      _warnTimer;
        private Coroutine  _warnRoutine;
        private Image      _warnFlashOverlay;
        private Image[]    _warnEdges;
        private Image      _vignetteImage;
        private Texture2D  _vignetteTex;
        private Image      _warnBGStrip;
        private Image      _warnSpriteImage;
        private Text       _warnMessageText;
        private Text       _warnIconText;
        private string     _dangerSentence = "";

        private static readonly string[] WarnMessages =
            { "請相信AI的判斷", "只須交給AI即可", "AI會幫你安排一切" };
        private const float WarnInterval = 60f;

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
            if (_vignetteTex != null) Destroy(_vignetteTex);
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
                // 邊緣緩慢脈動
                _pulseTimer += Time.deltaTime;
                float pulse = (Mathf.Sin(_pulseTimer * 1.5f) + 1f) * 0.5f;  // 0~1 慢速正弦
                _dangerOverlay.color = new Color(0.65f, 0f, 0f, 0.10f + (1f - pulse) * 0.08f);
                if (_vignetteImage != null) _vignetteImage.color = new Color(1f, 1f, 1f, pulse * 0.88f);

                // 圖片每 8 秒獨立出現一次
                _dangerCycleTimer += Time.deltaTime;
                if (_dangerCycleTimer >= 8f && _dangerImageRoutine == null)
                {
                    _dangerCycleTimer = 0f;
                    PickRandomWarnSprite();
                    _dangerImageRoutine = StartCoroutine(DangerImageFlashRoutine());
                }
            }

            // 周期閃紅：< 60 且「不在 < 10 危險狀態」時才計時
            if (_isWarn && !_isDanger && _warnRoutine == null)
            {
                _warnTimer += Time.deltaTime;
                if (_warnTimer >= WarnInterval)
                {
                    _warnTimer = 0f;
                    _warnRoutine = StartCoroutine(WarnFlashRoutine());
                }
            }
        }

        private System.Collections.IEnumerator DangerImageFlashRoutine()
        {
            for (int i = 0; i < 3; i++)
            {
                SetWarnTextAlpha(1f);
                yield return new WaitForSeconds(0.12f);
                SetWarnTextAlpha(0f);
                if (i < 2) yield return new WaitForSeconds(0.09f);
            }
            yield return new WaitForSeconds(0.18f);
            SetWarnTextAlpha(1f);
            yield return new WaitForSeconds(0.6f);
            float t = 0f;
            while (t < 1.8f)
            {
                t += Time.deltaTime;
                SetWarnTextAlpha(1f - Mathf.SmoothStep(0f, 1f, t / 1.8f));
                yield return null;
            }
            SetWarnTextAlpha(0f);
            _dangerImageRoutine = null;
        }

        private System.Collections.IEnumerator WarnFlashRoutine()
        {
            string msg = WarnMessages[UnityEngine.Random.Range(0, WarnMessages.Length)];
            PickRandomWarnSprite();
            if (_warnSpriteImage == null && _warnMessageText != null)
                _warnMessageText.text = "⚠  " + msg;

            // 快速閃三下（strobe）
            for (int i = 0; i < 3; i++)
            {
                SetWarnOverlayAlpha(1f);
                SetWarnTextAlpha(0.52f);
                yield return new WaitForSeconds(0.07f);
                SetWarnOverlayAlpha(0f);
                SetWarnTextAlpha(0f);
                if (i < 2) yield return new WaitForSeconds(0.05f);
            }

            // 短暫停頓後，最後一次亮起再慢慢淡出
            yield return new WaitForSeconds(0.12f);
            SetWarnOverlayAlpha(1f);
            SetWarnTextAlpha(0.52f);
            yield return new WaitForSeconds(0.3f);

            float t = 0f;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                float a = 1f - Mathf.SmoothStep(0f, 1f, t / 1.2f);
                SetWarnOverlayAlpha(a);
                SetWarnTextAlpha(a * 0.52f);
                yield return null;
            }
            SetWarnOverlayAlpha(0f);
            SetWarnTextAlpha(0f);
            _warnRoutine = null;
        }

        private void PickRandomWarnSprite()
        {
            if (_warnSpriteImage == null || warnSprites == null || warnSprites.Length == 0) return;
            var s = warnSprites[UnityEngine.Random.Range(0, warnSprites.Length)];
            if (s != null) _warnSpriteImage.sprite = s;
        }

        private void SetWarnTextAlpha(float alpha)
        {
            if (_warnSpriteImage != null)
            {
                _warnSpriteImage.color = new Color(1f, 1f, 1f, alpha);
                if (_warnBGStrip    != null) _warnBGStrip.color    = Color.clear;
                if (_warnMessageText != null) _warnMessageText.color = Color.clear;
            }
            else
            {
                var c = new Color(1f, 1f, 1f, alpha);
                if (_warnMessageText != null) _warnMessageText.color = c;
                if (_warnIconText    != null) _warnIconText.color    = c;
                if (_warnBGStrip    != null) _warnBGStrip.color = new Color(0.5f, 0f, 0f, alpha * 0.88f);
            }
        }

        private void SetWarnOverlayAlpha(float alpha)
        {
            if (_warnFlashOverlay != null)
                _warnFlashOverlay.color = new Color(0.5f, 0f, 0f, alpha * 0.25f);
            if (_vignetteImage != null)
                _vignetteImage.color = new Color(1f, 1f, 1f, alpha);
        }

        private void UpdateBrainPower(int current, int max)
        {
            float pct = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            if (_fillClipRt != null)
                _fillClipRt.sizeDelta = new Vector2(barWidth * pct, _fillClipRt.sizeDelta.y);
            else if (_brainFill != null)
                _brainFill.fillAmount = pct;
            if (_brainLabel != null) _brainLabel.text = $"{Mathf.RoundToInt(pct * 100)}%";

            bool wasDanger = _isDanger;
            _isDanger = current < 10;
            if (_isDanger && !wasDanger)
            {
                // 剛進入危險：抽一句、重置計時、停止周期閃紅
                _dangerSentence   = WarnMessages[UnityEngine.Random.Range(0, WarnMessages.Length)];
                PickRandomWarnSprite();
                _dangerCycleTimer = 0f;
                _pulseTimer       = 0f;
                if (_warnRoutine != null) { StopCoroutine(_warnRoutine); _warnRoutine = null; }
                if (_dangerImageRoutine != null) { StopCoroutine(_dangerImageRoutine); _dangerImageRoutine = null; }
                _warnTimer = 0f;
                SetWarnOverlayAlpha(0f);
                SetWarnTextAlpha(0f);
            }
            if (_dangerOverlay != null && !_isDanger)
            {
                _dangerOverlay.color = new Color(0f, 0f, 0f, 0f);
                if (_vignetteImage != null) _vignetteImage.color = Color.clear;
                if (_dangerImageRoutine != null) { StopCoroutine(_dangerImageRoutine); _dangerImageRoutine = null; }
                SetWarnTextAlpha(0f);
            }

            bool wasWarn = _isWarn;
            _isWarn = current < 60;
            if (!_isWarn && wasWarn)
            {
                _warnTimer = 0f;
                if (_warnRoutine != null) { StopCoroutine(_warnRoutine); _warnRoutine = null; }
                SetWarnOverlayAlpha(0f);
                SetWarnTextAlpha(0f);
            }

            if (_brainFill != null && barFillSprite == null)
                _brainFill.color = _isDanger ? new Color(1f, 0.2f, 0.2f, 1f) : FillColor;
        }

        private Texture2D CreateVignetteTexture()
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode  = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = Mathf.Abs((x - half) / half);
                float ny = Mathf.Abs((y - half) / half);
                // 矩形距離（邊緣均勻漸層，不偏角落）
                float dist = Mathf.Max(nx, ny);
                float a = Mathf.Clamp01((dist - 0.72f) / 0.28f);
                a = a * a * a;  // 三次方緩入，讓中心更乾淨
                pixels[y * size + x] = new Color(0.6f, 0.02f, 0.02f, a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
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

            // 危險覆蓋層（< 10 持續脈動）
            var overlayGo = new GameObject("DangerOverlay", typeof(Image));
            overlayGo.transform.SetParent(canvasGo.transform, false);
            _dangerOverlay = overlayGo.GetComponent<Image>();
            _dangerOverlay.color        = new Color(0f, 0f, 0f, 0f);
            _dangerOverlay.raycastTarget = false;
            var oRt = overlayGo.GetComponent<RectTransform>();
            oRt.anchorMin = Vector2.zero;
            oRt.anchorMax = Vector2.one;
            oRt.offsetMin = oRt.offsetMax = Vector2.zero;

            // 警告閃紅層（< 60，一分鐘一次）
            var warnOvGo = new GameObject("WarnFlashOverlay", typeof(Image));
            warnOvGo.transform.SetParent(canvasGo.transform, false);
            _warnFlashOverlay = warnOvGo.GetComponent<Image>();
            _warnFlashOverlay.color         = new Color(0.7f, 0f, 0f, 0f);
            _warnFlashOverlay.raycastTarget  = false;
            var wRt = warnOvGo.GetComponent<RectTransform>();
            wRt.anchorMin = Vector2.zero; wRt.anchorMax = Vector2.one;
            wRt.offsetMin = wRt.offsetMax = Vector2.zero;

            // 漸層 vignette（程式碼生成貼圖，邊緣到透明的真正漸層）
            _vignetteTex = CreateVignetteTexture();
            var vigGo = new GameObject("WarnVignette", typeof(Image));
            vigGo.transform.SetParent(canvasGo.transform, false);
            _vignetteImage = vigGo.GetComponent<Image>();
            _vignetteImage.sprite = Sprite.Create(_vignetteTex,
                new Rect(0, 0, _vignetteTex.width, _vignetteTex.height),
                new Vector2(0.5f, 0.5f));
            _vignetteImage.color         = Color.clear;
            _vignetteImage.raycastTarget = false;
            var vigRt = vigGo.GetComponent<RectTransform>();
            vigRt.anchorMin = Vector2.zero; vigRt.anchorMax = Vector2.one;
            vigRt.offsetMin = vigRt.offsetMax = Vector2.zero;

            // 警告圖片（有指定 Sprites 時使用）
            if (warnSprites != null && warnSprites.Length > 0 && warnSprites[0] != null)
            {
                var wsGo = new GameObject("WarnSprite", typeof(Image));
                wsGo.transform.SetParent(canvasGo.transform, false);
                _warnSpriteImage = wsGo.GetComponent<Image>();
                _warnSpriteImage.sprite         = warnSprites[0];
                _warnSpriteImage.color          = new Color(1f, 1f, 1f, 0f);
                _warnSpriteImage.preserveAspect = false;
                _warnSpriteImage.raycastTarget  = false;
                var wsRt = wsGo.GetComponent<RectTransform>();
                wsRt.anchorMin = Vector2.zero;
                wsRt.anchorMax = Vector2.one;
                wsRt.offsetMin = wsRt.offsetMax = Vector2.zero;
            }

            // 文字底色橫帶
            var bgStripGo = new GameObject("WarnBGStrip", typeof(Image));
            bgStripGo.transform.SetParent(canvasGo.transform, false);
            _warnBGStrip = bgStripGo.GetComponent<Image>();
            _warnBGStrip.color         = new Color(0f, 0f, 0f, 0f);
            _warnBGStrip.raycastTarget = false;
            var bsRt = bgStripGo.GetComponent<RectTransform>();
            bsRt.anchorMin        = new Vector2(0f, 0.5f);
            bsRt.anchorMax        = new Vector2(1f, 0.5f);
            bsRt.pivot            = new Vector2(0.5f, 0.5f);
            bsRt.anchoredPosition = new Vector2(0f, 120f);
            bsRt.sizeDelta        = new Vector2(0f, 340f);

            // 警告文字（⚠ 已內嵌在字串裡，置中顯示）
            var warnTxtGo = new GameObject("WarnMessage", typeof(Text));
            warnTxtGo.transform.SetParent(canvasGo.transform, false);
            _warnMessageText = warnTxtGo.GetComponent<Text>();
            _warnMessageText.font              = _font;
            _warnMessageText.fontSize          = 96;
            _warnMessageText.alignment         = TextAnchor.MiddleCenter;
            _warnMessageText.color             = new Color(1f, 1f, 1f, 0f);
            _warnMessageText.raycastTarget     = false;
            _warnMessageText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _warnMessageText.verticalOverflow   = VerticalWrapMode.Overflow;
            var outline = warnTxtGo.AddComponent<Outline>();
            outline.effectColor    = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            var wtRt = warnTxtGo.GetComponent<RectTransform>();
            wtRt.anchorMin = wtRt.anchorMax = wtRt.pivot = new Vector2(0.5f, 0.5f);
            wtRt.anchoredPosition = new Vector2(0f, 120f);
            wtRt.sizeDelta        = new Vector2(1600f, 130f);
        }
    }
}
