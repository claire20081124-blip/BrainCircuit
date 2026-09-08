using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using RunLight.Interaction;

namespace RunLight.UI
{
    public class BrainQAUI : MonoBehaviour
    {
        private static BrainQAUI _instance;
        public static BrainQAUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("BrainQAUI");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<BrainQAUI>();
                }
                return _instance;
            }
        }

        private const float DefaultTimeLimit = 5f;

        private GameObject   _panel;
        private Canvas       _canvas;
        private Text         _sysLabel;
        private Text         _questionText;
        private Text[]       _btnTexts = new Text[2];
        private Button[]     _buttons  = new Button[2];

        // 計時器
        private Image        _timerFill;
        private Text         _timerText;
        private float        _timeRemaining;
        private float        _timeTotal;
        private bool         _timerRunning;

        private BrainQuestion _currentQ;
        private bool          _isBadBrain;
        private Action<bool>  _callback;
        private Font          _font;

        private static readonly Color PanelBg   = new(0.04f, 0.06f, 0.10f, 0.95f);
        private static readonly Color OverlayBg = new(0f,    0f,    0f,    0.70f);
        private static readonly Color AccentCol  = new(0.55f, 0.80f, 1.00f, 0.85f);
        private static readonly Color TimerFull  = new(0.20f, 0.80f, 0.30f, 1.00f);
        private static readonly Color TimerMid   = new(0.90f, 0.75f, 0.10f, 1.00f);
        private static readonly Color TimerLow   = new(0.90f, 0.20f, 0.10f, 1.00f);

        public static bool IsOpen { get; private set; }

        internal static void Lock() { IsOpen = true; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetIsOpen() { IsOpen = false; }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
        }

        private void Update()
        {
            if (_panel == null || !_panel.activeSelf) return;

            // 計時器更新
            if (_timerRunning)
            {
                _timeRemaining -= Time.deltaTime;
                float ratio = Mathf.Clamp01(_timeRemaining / _timeTotal);

                // 縮短計時條（anchorMax.x）
                if (_timerFill != null)
                {
                    var rt = _timerFill.rectTransform;
                    rt.anchorMax = new Vector2(ratio, rt.anchorMax.y);
                    _timerFill.color = ratio > 0.5f
                        ? Color.Lerp(TimerMid, TimerFull, (ratio - 0.5f) * 2f)
                        : Color.Lerp(TimerLow, TimerMid, ratio * 2f);
                }

                if (_timerText != null)
                    _timerText.text = Mathf.CeilToInt(Mathf.Max(0f, _timeRemaining)).ToString();

                if (_timeRemaining <= 0f)
                {
                    _timerRunning = false;
                    Answer(-1);   // 逾時 → 判錯
                }
            }

            if (Input.GetKeyDown(KeyCode.Z)) Answer(0);
            if (Input.GetKeyDown(KeyCode.X)) Answer(1);

            if (Input.GetMouseButtonDown(0))
            {
                for (int i = 0; i < _buttons.Length; i++)
                {
                    if (_buttons[i] == null) continue;
                    var rt = _buttons[i].GetComponent<RectTransform>();
                    if (RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null))
                    {
                        Answer(i);
                        break;
                    }
                }
            }
        }

        public void FlyAndShow(Vector2 startScreen, Sprite icon,
            BrainQuestion q, bool isBadBrain, Action<bool> callback)
        {
            StartCoroutine(FlyRoutine(startScreen, icon, q, isBadBrain, callback));
        }

        private IEnumerator FlyRoutine(Vector2 startScreen, Sprite icon,
            BrainQuestion q, bool isBadBrain, Action<bool> callback)
        {
            var canvasRt = _canvas.GetComponent<RectTransform>();

            var fxGo = new GameObject("PickupFX", typeof(Image));
            fxGo.transform.SetParent(_canvas.transform, false);
            fxGo.transform.SetAsLastSibling();
            var fxImg = fxGo.GetComponent<Image>();
            if (icon != null) { fxImg.sprite = icon; fxImg.preserveAspect = true; fxImg.color = Color.white; }
            else               { fxImg.color = AccentCol; }

            var fxRt = fxGo.GetComponent<RectTransform>();
            fxRt.anchorMin = fxRt.anchorMax = fxRt.pivot = new Vector2(0.5f, 0.5f);
            fxRt.sizeDelta = new Vector2(100f, 100f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, startScreen, null, out Vector2 startLocal);
            fxRt.anchoredPosition = startLocal;
            fxRt.localScale       = Vector3.one * 1.5f;

            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                float pct = Mathf.SmoothStep(0f, 1f, t / 0.5f);
                fxRt.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.zero, pct);
                yield return null;
            }

            Destroy(fxGo);
            Show(q, isBadBrain, callback);
        }

        public void Show(BrainQuestion q, bool isBadBrain, Action<bool> callback)
        {
            _currentQ   = q;
            _isBadBrain = isBadBrain;
            _callback   = callback;

            _sysLabel.text     = isBadBrain ? "認知反應測試  ██ 訊號異常" : "認知反應測試";
            _questionText.text = q.question;
            _btnTexts[0].text  = "✓";
            _btnTexts[1].text  = "✗";

            // 重設計時器
            _timeTotal     = q.timeLimit > 0f ? q.timeLimit : DefaultTimeLimit;
            _timeRemaining = _timeTotal;
            _timerRunning  = true;

            if (_timerFill != null)
            {
                var rt = _timerFill.rectTransform;
                rt.anchorMax = new Vector2(1f, rt.anchorMax.y);
                _timerFill.color = TimerFull;
            }
            if (_timerText != null)
                _timerText.text = Mathf.CeilToInt(_timeTotal).ToString();

            IsOpen = true;
            _panel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        private void Answer(int index)
        {
            _timerRunning = false;
            IsOpen        = false;
            _panel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null) es.SetSelectedGameObject(null);

            // index -1 = 逾時（判錯），0 = 是，1 = 否
            bool correct = index != -1 && !_isBadBrain && ((index == 0) == _currentQ.correctAnswer);
            _callback?.Invoke(correct);
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("BrainQACanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 30;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            // 全螢幕遮罩
            _panel = new GameObject("QAPanel", typeof(Image));
            _panel.transform.SetParent(canvasGo.transform, false);
            var panelImg = _panel.GetComponent<Image>();
            panelImg.color = OverlayBg;
            panelImg.raycastTarget = false;
            Stretch(_panel.GetComponent<RectTransform>());

            // 對話框
            var box = new GameObject("Box", typeof(Image));
            box.transform.SetParent(_panel.transform, false);
            box.GetComponent<Image>().color = PanelBg;
            box.GetComponent<Image>().raycastTarget = false;
            var boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = boxRt.anchorMax = boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(960f, 520f);

            // 系統標題
            _sysLabel = MakeTxt("SysLabel", box.transform, "認知反應測試", 26, AccentCol, FontStyle.Normal);
            var sRt = _sysLabel.rectTransform;
            sRt.anchorMin = new Vector2(0f, 1f); sRt.anchorMax = new Vector2(1f, 1f);
            sRt.pivot     = new Vector2(0.5f, 1f);
            sRt.anchoredPosition = new Vector2(0f, -28f);
            sRt.sizeDelta        = new Vector2(-80f, 36f);
            _sysLabel.alignment  = TextAnchor.MiddleCenter;

            // 分隔線
            var line = new GameObject("Line", typeof(Image));
            line.transform.SetParent(box.transform, false);
            line.GetComponent<Image>().color = new Color(AccentCol.r, AccentCol.g, AccentCol.b, 0.25f);
            var lineRt = line.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0f, 1f); lineRt.anchorMax = new Vector2(1f, 1f);
            lineRt.pivot     = new Vector2(0.5f, 1f);
            lineRt.anchoredPosition = new Vector2(0f, -70f);
            lineRt.sizeDelta        = new Vector2(-80f, 2f);

            // 題目文字
            _questionText = MakeTxt("Question", box.transform, "", 42, Color.white, FontStyle.Normal);
            var qRt = _questionText.rectTransform;
            qRt.anchorMin = new Vector2(0f, 0.48f); qRt.anchorMax = new Vector2(1f, 0.88f);
            qRt.offsetMin = new Vector2(60f, 0f);   qRt.offsetMax = new Vector2(-60f, 0f);
            _questionText.alignment          = TextAnchor.MiddleCenter;
            _questionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _questionText.verticalOverflow   = VerticalWrapMode.Overflow;

            // ── 計時區域（題目正下方）──
            // 倒數秒數
            _timerText = MakeTxt("TimerText", box.transform, "10", 24,
                new Color(1f, 1f, 1f, 0.80f), FontStyle.Bold);
            var ttRt = _timerText.rectTransform;
            ttRt.anchorMin = new Vector2(0f, 0.38f); ttRt.anchorMax = new Vector2(1f, 0.46f);
            ttRt.offsetMin = new Vector2(60f, 0f);   ttRt.offsetMax = new Vector2(-60f, 0f);
            _timerText.alignment = TextAnchor.MiddleCenter;

            // 計時條背景（細條，固定 6px 高）
            var timerTrackGo = new GameObject("TimerTrack", typeof(Image));
            timerTrackGo.transform.SetParent(box.transform, false);
            timerTrackGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            var trackRt = timerTrackGo.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0f, 0.36f); trackRt.anchorMax = new Vector2(1f, 0.36f);
            trackRt.pivot     = new Vector2(0.5f, 0.5f);
            trackRt.anchoredPosition = Vector2.zero;
            trackRt.sizeDelta = new Vector2(-120f, 6f);

            var timerFillGo = new GameObject("TimerFill", typeof(Image));
            timerFillGo.transform.SetParent(timerTrackGo.transform, false);
            _timerFill = timerFillGo.GetComponent<Image>();
            _timerFill.color = TimerFull;
            var fillRt = _timerFill.rectTransform;
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

            // ✓ / ✗ 兩個按鈕
            float[]  xPos      = { -200f, 200f };
            string[] btnNames  = { "BtnYes", "BtnNo" };
            Color[]  btnColors = {
                new Color(0.10f, 0.45f, 0.15f, 0.92f),
                new Color(0.45f, 0.10f, 0.10f, 0.92f)
            };
            Color[] hlColors = {
                new Color(0.20f, 0.75f, 0.25f, 1.00f),
                new Color(0.75f, 0.20f, 0.20f, 1.00f)
            };

            for (int i = 0; i < 2; i++)
            {
                int idx = i;
                var btnGo = new GameObject(btnNames[i], typeof(Image), typeof(Button));
                btnGo.transform.SetParent(box.transform, false);
                btnGo.GetComponent<Image>().color = btnColors[i];
                var btnRt = btnGo.GetComponent<RectTransform>();
                btnRt.anchorMin = btnRt.anchorMax = btnRt.pivot = new Vector2(0.5f, 0f);
                btnRt.anchoredPosition = new Vector2(xPos[i], 36f);
                btnRt.sizeDelta        = new Vector2(200f, 120f);

                var btn = btnGo.GetComponent<Button>();
                var cols = btn.colors;
                cols.normalColor      = Color.white;
                cols.highlightedColor = hlColors[i];
                cols.pressedColor     = hlColors[i] * 0.7f;
                cols.fadeDuration     = 0.08f;
                btn.colors = cols;
                btn.onClick.AddListener(() => Answer(idx));
                _buttons[i] = btn;

                var txt = MakeTxt(btnNames[i] + "Label", btnGo.transform, "", 42, Color.white, FontStyle.Bold);
                var tRt = txt.rectTransform;
                tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
                tRt.offsetMin = tRt.offsetMax = Vector2.zero;
                txt.alignment        = TextAnchor.MiddleCenter;
                txt.verticalOverflow = VerticalWrapMode.Overflow;
                _btnTexts[i] = txt;
            }

            _panel.SetActive(false);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private Text MakeTxt(string name, Transform parent, string content,
            int size, Color color, FontStyle style)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font      = _font;
            t.text      = content;
            t.fontSize  = size;
            t.color     = color;
            t.fontStyle = style;
            t.raycastTarget = false;
            return t;
        }
    }
}
