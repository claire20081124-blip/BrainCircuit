using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using RunLight.Player;
using RunLight.Core;


namespace RunLight.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }

        [Header("外觀")]
        [SerializeField] private string backgroundSpriteName = "ee";

        [Header("操作說明圖（有圖時取代文字表格）")]
        [SerializeField] private Sprite controlsGuideSprite;

        private GameObject _pauseCanvas;
        private GameObject _pausePanel;
        private GameObject _settingsPanel;
        private GameObject[] _tabContents;
        private Font        _font;
        private FirstPersonController _fpc;

        private static readonly string[] TabNames = { "案件說明", "音量", "靈敏度" };
        private static readonly string[] TabIcons = { "🔑", "🔊", "🎯" };

        private static readonly (string key, string action)[] KeyBindings =
        {
            ("W A S D", "移動"),
            ("E",        "互動 / 確定"),
            ("Q",        "取消 / 確定"),
            ("Shift",    "跳躍 / 互動"),
            ("Tab",      "打開選單"),
            ("Esc",      "暫停 / 繼續"),
        };

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
        }

        private void Start()
        {
            _fpc = FindObjectOfType<FirstPersonController>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();
        }

        // ── 暫停控制 ──────────────────────────────────────────────

        private void TogglePause()
        {
            if (IsPaused) Resume(); else Pause();
        }

        private void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            _pauseCanvas.SetActive(true);
            _pausePanel.SetActive(true);
            _settingsPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        private void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            _pauseCanvas.SetActive(false);
            _pausePanel.SetActive(false);
            _settingsPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void OpenSettings()
        {
            _pausePanel.SetActive(false);
            _settingsPanel.SetActive(true);
            ShowTab(0);
        }

        private void BackToPause()
        {
            _settingsPanel.SetActive(false);
            _pausePanel.SetActive(true);
        }

        private void QuitGame()
        {
            Debug.Log("[PauseMenu] QuitGame 被呼叫");
            {
                var gm  = GameManager.Instance;
                var fpc = _fpc != null ? _fpc : FindObjectOfType<FirstPersonController>();
                if (fpc != null)
                    gm.SaveGame(0, fpc.transform.position, fpc.transform.eulerAngles.y);
                else
                    gm.SaveGame(0);
                Debug.Log("[PauseMenu] 存檔完成");
            }
            IsPaused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }

        // ── Tab 切換 ──────────────────────────────────────────────

        private void ShowTab(int index)
        {
            for (int i = 0; i < _tabContents.Length; i++)
                _tabContents[i].SetActive(i == index);
        }

        // ── 音量 / 靈敏度 ─────────────────────────────────────────

        private void OnVolumeChanged(float v)    => AudioListener.volume = v;

        private void OnSensitivityChanged(float v)
        {
            if (_fpc == null) _fpc = FindObjectOfType<FirstPersonController>();
            _fpc?.SetSensitivity(v);
        }

        // ── 建立 UI ───────────────────────────────────────────────

        private void BuildUI()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            var canvasGo = new GameObject("PauseCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            // 背景
            if (!string.IsNullOrEmpty(backgroundSpriteName))
            {
                var tex = Resources.Load<Texture2D>(backgroundSpriteName);
                if (tex != null)
                {
                    var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    var bg = MakeImage(canvasGo.transform, Color.white, Vector2.zero, Vector2.one);
                    var bgImg = bg.GetComponent<Image>();
                    bgImg.sprite = spr;
                    bgImg.type   = Image.Type.Simple;
                }
            }

            _pauseCanvas   = canvasGo;
            _pausePanel    = BuildPausePanel(canvasGo.transform);
            _settingsPanel = BuildSettingsPanel(canvasGo.transform);

            _pausePanel.SetActive(false);
            _settingsPanel.SetActive(false);
            canvasGo.SetActive(false);
        }

        // ── 暫停面板 ──────────────────────────────────────────────

        private GameObject BuildPausePanel(Transform root)
        {
            var panel = new GameObject("PausePanel", typeof(Image));
            panel.transform.SetParent(root, false);
            panel.GetComponent<Image>().color = Color.clear;
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            MakeLabel(panel.transform, "暫停", 56, Color.white, new Vector2(0.5f, 0.84f), new Vector2(300f, 70f));
            MakeLabel(panel.transform, "PAUSE", 18, new Color(1f,1f,1f,0.35f), new Vector2(0.5f, 0.77f), new Vector2(200f, 26f));

            float[] yAnchors = { 0.60f, 0.42f, 0.22f };
            string[] labels  = { "繼續", "設定", "離開遊戲" };
            System.Action[] actions = { Resume, OpenSettings, QuitGame };

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                MakePanelButton(panel.transform, labels[i], yAnchors[i],
                    new Color(0.15f, 0.15f, 0.2f, 0.9f), actions[idx]);
            }

            return panel;
        }

        // ── 設定面板 ──────────────────────────────────────────────

        private GameObject BuildSettingsPanel(Transform root)
        {
            var panel = MakePanel(root, new Vector2(960f, 640f));

            // ── 左側 Tab 欄 ──
            var sidebar = new GameObject("Sidebar", typeof(Image));
            sidebar.transform.SetParent(panel.transform, false);
            sidebar.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 1f);
            var sbRt = sidebar.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0f, 0f);
            sbRt.anchorMax = new Vector2(0.28f, 1f);
            sbRt.offsetMin = sbRt.offsetMax = Vector2.zero;

            // ── 右側 內容區 ──
            var contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(panel.transform, false);
            var caRt = contentArea.AddComponent<RectTransform>();
            caRt.anchorMin = new Vector2(0.30f, 0.08f);
            caRt.anchorMax = new Vector2(0.98f, 0.90f);
            caRt.offsetMin = caRt.offsetMax = Vector2.zero;

            _tabContents = new GameObject[TabNames.Length];
            float tabSlotH = 0.18f;

            for (int i = 0; i < TabNames.Length; i++)
            {
                int idx = i;
                float yMax = 0.95f - tabSlotH * i;
                float yMin = yMax - tabSlotH + 0.02f;

                // 側邊 Tab 按鈕
                var tabBtn = new GameObject("Tab_" + i, typeof(Image), typeof(Button));
                tabBtn.transform.SetParent(sidebar.transform, false);
                tabBtn.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.20f, 1f);
                tabBtn.GetComponent<Button>().onClick.AddListener(() => ShowTab(idx));
                var rt = tabBtn.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.04f, yMin);
                rt.anchorMax = new Vector2(0.96f, yMax);
                rt.offsetMin = rt.offsetMax = Vector2.zero;

                // 圖示
                var icon = new GameObject("Icon", typeof(Text));
                icon.transform.SetParent(tabBtn.transform, false);
                var it = icon.GetComponent<Text>();
                it.font = _font; it.text = TabIcons[i]; it.fontSize = 20;
                it.color = new Color(1f,1f,1f,0.7f); it.alignment = TextAnchor.MiddleCenter;
                it.raycastTarget = false;
                var irt = icon.GetComponent<RectTransform>();
                irt.anchorMin = new Vector2(0f, 0f); irt.anchorMax = new Vector2(0.3f, 1f);
                irt.offsetMin = irt.offsetMax = Vector2.zero;

                // Tab 名稱
                var lbl = new GameObject("L", typeof(Text));
                lbl.transform.SetParent(tabBtn.transform, false);
                var t = lbl.GetComponent<Text>();
                t.font = _font; t.text = TabNames[i]; t.fontSize = 20;
                t.color = Color.white; t.alignment = TextAnchor.MiddleLeft;
                t.raycastTarget = false;
                var lrt = lbl.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0.3f, 0f); lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(4f,0f); lrt.offsetMax = Vector2.zero;

                // 內容區
                var content = new GameObject("Content_" + i);
                content.transform.SetParent(contentArea.transform, false);
                var crt = content.AddComponent<RectTransform>();
                crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
                crt.offsetMin = crt.offsetMax = Vector2.zero;
                _tabContents[i] = content;

                BuildTabContent(content.transform, i);
            }

            // 返回按鈕（左下）
            var backBtn = new GameObject("Back", typeof(Image), typeof(Button));
            backBtn.transform.SetParent(sidebar.transform, false);
            backBtn.GetComponent<Image>().color = new Color(0.3f, 0.12f, 0.12f, 0.9f);
            backBtn.GetComponent<Button>().onClick.AddListener(BackToPause);
            var brt = backBtn.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.04f, 0.02f); brt.anchorMax = new Vector2(0.96f, 0.10f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            var blbl = new GameObject("L", typeof(Text));
            blbl.transform.SetParent(backBtn.transform, false);
            var bt = blbl.GetComponent<Text>();
            bt.font = _font; bt.text = "← 返回"; bt.fontSize = 18;
            bt.color = Color.white; bt.alignment = TextAnchor.MiddleCenter;
            bt.raycastTarget = false;
            var blrt = blbl.GetComponent<RectTransform>();
            blrt.anchorMin = Vector2.zero; blrt.anchorMax = Vector2.one;
            blrt.offsetMin = blrt.offsetMax = Vector2.zero;

            return panel;
        }

        private void BuildTabContent(Transform parent, int tab)
        {
            switch (tab)
            {
                case 0: BuildKeyBindings(parent); break;
                case 1: BuildVolumeTab(parent);   break;
                case 2: BuildSensTab(parent);     break;
            }
        }

        // 按鍵說明
        private void BuildKeyBindings(Transform parent)
        {
            if (controlsGuideSprite != null)
            {
                var go = new GameObject("ControlsImg", typeof(Image));
                go.transform.SetParent(parent, false);
                var img = go.GetComponent<Image>();
                img.sprite = controlsGuideSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.05f, 0.05f);
                rt.anchorMax = new Vector2(0.95f, 0.95f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                return;
            }

            float rowH = 1f / (KeyBindings.Length + 1);
            MakeRowLabel(parent, "按鍵", 0f, 1f - rowH * 0.5f, true);
            MakeRowLabel(parent, "功能", 0.45f, 1f - rowH * 0.5f, true);

            for (int i = 0; i < KeyBindings.Length; i++)
            {
                float y = 1f - rowH * (i + 1.5f);
                MakeRowLabel(parent, KeyBindings[i].key,    0f,    y, false);
                MakeRowLabel(parent, KeyBindings[i].action, 0.45f, y, false);
            }
        }

        private void MakeRowLabel(Transform parent, string text, float xAnchor, float yAnchor, bool header)
        {
            var go = new GameObject("R", typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font      = _font; t.text = text;
            t.fontSize  = header ? 20 : 18;
            t.fontStyle = header ? FontStyle.Bold : FontStyle.Normal;
            t.color     = header ? new Color(0.7f, 0.85f, 1f) : Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(xAnchor, yAnchor - 0.05f);
            rt.anchorMax        = new Vector2(xAnchor + 0.44f, yAnchor + 0.05f);
            rt.offsetMin        = new Vector2(8f, 0f);
            rt.offsetMax        = Vector2.zero;
        }

        // 音量
        private void BuildVolumeTab(Transform parent)
        {
            MakeSliderRow(parent, "主音量", 0.65f, 0f, 1f, AudioListener.volume, OnVolumeChanged);
        }

        // 靈敏度
        private void BuildSensTab(Transform parent)
        {
            float cur = FindObjectOfType<FirstPersonController>()?.GetSensitivity() ?? 0.15f;
            MakeSliderRow(parent, "滑鼠靈敏度", 0.65f, 0.03f, 0.5f, cur, OnSensitivityChanged);
        }


        private void MakeSliderRow(Transform parent, string label, float yAnchor,
            float min, float max, float current,
            UnityEngine.Events.UnityAction<float> onChange)
        {
            // 文字標籤
            var lbl = new GameObject("Lbl", typeof(Text));
            lbl.transform.SetParent(parent, false);
            var lt = lbl.GetComponent<Text>();
            lt.font = _font; lt.text = label; lt.fontSize = 22;
            lt.color = Color.white; lt.alignment = TextAnchor.MiddleLeft;
            lt.raycastTarget = false;
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.05f, yAnchor);
            lrt.anchorMax = new Vector2(0.4f,  yAnchor + 0.12f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            // Slider
            var sliderGo = new GameObject("Slider", typeof(Slider));
            sliderGo.transform.SetParent(parent, false);
            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = min; slider.maxValue = max; slider.value = current;
            slider.onValueChanged.AddListener(onChange);
            var srt = sliderGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.42f, yAnchor + 0.02f);
            srt.anchorMax = new Vector2(0.95f, yAnchor + 0.10f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;

            // Slider 背景
            var bg = new GameObject("BG", typeof(Image));
            bg.transform.SetParent(sliderGo.transform, false);
            bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);
            var bgrt = bg.GetComponent<RectTransform>();
            bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one;
            bgrt.offsetMin = bgrt.offsetMax = Vector2.zero;
            slider.targetGraphic = bg.GetComponent<Image>();

            // Fill area
            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fart = fillArea.AddComponent<RectTransform>();
            fart.anchorMin = Vector2.zero; fart.anchorMax = Vector2.one;
            fart.offsetMin = fart.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill", typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = new Color(0.25f, 0.7f, 1f);
            var frt = fill.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(current / max, 1f);
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            slider.fillRect = frt;

            // Handle
            var handleArea = new GameObject("HandleArea");
            handleArea.transform.SetParent(sliderGo.transform, false);
            var hart = handleArea.AddComponent<RectTransform>();
            hart.anchorMin = Vector2.zero; hart.anchorMax = Vector2.one;
            hart.offsetMin = hart.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle", typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            handle.GetComponent<Image>().color = Color.white;
            var hrt = handle.GetComponent<RectTransform>();
            hrt.sizeDelta = new Vector2(20f, 0f);
            slider.handleRect = hrt;
        }

        // ── 通用輔助 ──────────────────────────────────────────────

        private GameObject MakePanel(Transform root, Vector2 size)
        {
            var go = new GameObject("Panel", typeof(Image));
            go.transform.SetParent(root, false);
            go.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            return go;
        }

        private void MakeLabel(Transform parent, string text, int size, Color color,
            Vector2 anchor, Vector2 sizeDelta)
        {
            var go = new GameObject("Lbl", typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = _font; t.text = text; t.fontSize = size;
            t.color = color; t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = FontStyle.Bold; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = sizeDelta;
        }

        private void MakePanelButton(Transform parent, string label, float yAnchor,
            Color bg, System.Action onClick, float width = 0.7f)
        {
            var go = new GameObject("Btn", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bg;
            go.GetComponent<Button>().onClick.AddListener(() => onClick());
            var rt = go.GetComponent<RectTransform>();
            float margin = (1f - width) / 2f;
            rt.anchorMin = new Vector2(margin, yAnchor - 0.07f);
            rt.anchorMax = new Vector2(1f - margin, yAnchor + 0.07f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var lbl = new GameObject("L", typeof(Text));
            lbl.transform.SetParent(go.transform, false);
            var t = lbl.GetComponent<Text>();
            t.font = _font; t.text = label; t.fontSize = 24;
            t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        }

        private GameObject MakeImage(Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Img", typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }
    }
}
