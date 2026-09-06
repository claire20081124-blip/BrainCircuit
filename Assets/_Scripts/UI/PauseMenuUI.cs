using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RunLight.Player;
using RunLight.Core;
using RunLight.Save;

namespace RunLight.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }

        private GameObject _pausePanel;
        private GameObject _settingsPanel;
        private GameObject[] _tabContents;  // 0=按鍵說明 1=音量 2=靈敏度 3=存檔
        private Text[]      _slotLabels = new Text[SaveSystem.MaxSlots];
        private Font        _font;
        private FirstPersonController _fpc;

        private static readonly string[] TabNames = { "按鍵說明", "音量", "靈敏度", "存檔" };

        private static readonly (string key, string action)[] KeyBindings =
        {
            ("W A S D",  "移動"),
            ("Q",        "快跑"),
            ("空白鍵",   "互動（吃腦子）"),
            ("Tab",      "顯示 / 隱藏滑鼠"),
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
            _pausePanel.SetActive(true);
            _settingsPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        private void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
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
            RefreshSlotLabels();
        }

        private void BackToPause()
        {
            _settingsPanel.SetActive(false);
            _pausePanel.SetActive(true);
        }

        private void QuitGame()
        {
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

        // ── 存檔 ──────────────────────────────────────────────────

        private void DoSave(int slot)
        {
            var data = new SaveData
            {
                slot         = slot,
                currentScene = SceneManager.GetActiveScene().name,
                playSeconds  = Time.realtimeSinceStartup,
            };

            SaveSystem.Save(data);
            RefreshSlotLabels();
        }

        private void RefreshSlotLabels()
        {
            for (int i = 0; i < SaveSystem.MaxSlots; i++)
            {
                if (_slotLabels[i] == null) continue;
                var d = SaveSystem.PeekSummary(i);
                _slotLabels[i].text = d != null ? d.DisplaySummary() : $"槽 {i}  （空）";
            }
        }

        // ── 建立 UI ───────────────────────────────────────────────

        private void BuildUI()
        {
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

            // 半透明背景
            var bg = MakeImage(canvasGo.transform, new Color(0f, 0f, 0f, 0.6f),
                Vector2.zero, Vector2.one);

            _pausePanel    = BuildPausePanel(canvasGo.transform);
            _settingsPanel = BuildSettingsPanel(canvasGo.transform);

            _pausePanel.SetActive(false);
            _settingsPanel.SetActive(false);
        }

        // ── 暫停面板 ──────────────────────────────────────────────

        private GameObject BuildPausePanel(Transform root)
        {
            var panel = MakePanel(root, new Vector2(420f, 420f));

            MakeLabel(panel.transform, "暫停", 48, Color.white, new Vector2(0.5f, 0.82f), new Vector2(280f, 60f));

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
            var panel = MakePanel(root, new Vector2(900f, 620f));

            MakeLabel(panel.transform, "設定", 40, Color.white, new Vector2(0.5f, 0.90f), new Vector2(600f, 50f));

            // Tab 按鈕
            _tabContents = new GameObject[TabNames.Length];
            float tabW   = 1f / TabNames.Length;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int idx = i;
                var tabBtn = new GameObject("Tab_" + i, typeof(Image), typeof(Button));
                tabBtn.transform.SetParent(panel.transform, false);
                tabBtn.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.28f, 1f);
                tabBtn.GetComponent<Button>().onClick.AddListener(() => ShowTab(idx));
                var rt = tabBtn.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(tabW * i + 0.01f, 0.74f);
                rt.anchorMax        = new Vector2(tabW * (i + 1) - 0.01f, 0.83f);
                rt.offsetMin        = rt.offsetMax = Vector2.zero;

                var lbl = new GameObject("L", typeof(Text));
                lbl.transform.SetParent(tabBtn.transform, false);
                var t = lbl.GetComponent<Text>();
                t.font    = _font; t.text = TabNames[i]; t.fontSize = 20;
                t.color   = Color.white; t.alignment = TextAnchor.MiddleCenter;
                t.raycastTarget = false;
                var lrt = lbl.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;

                // Tab 內容區
                var content = new GameObject("Content_" + i);
                content.transform.SetParent(panel.transform, false);
                var crt = content.AddComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.02f, 0.12f);
                crt.anchorMax = new Vector2(0.98f, 0.72f);
                crt.offsetMin = crt.offsetMax = Vector2.zero;
                _tabContents[i] = content;

                BuildTabContent(content.transform, i);
            }

            // 返回按鈕
            MakePanelButton(panel.transform, "← 返回", 0.05f,
                new Color(0.3f, 0.15f, 0.15f, 0.9f), BackToPause, width: 0.3f);

            return panel;
        }

        private void BuildTabContent(Transform parent, int tab)
        {
            switch (tab)
            {
                case 0: BuildKeyBindings(parent);   break;
                case 1: BuildVolumeTab(parent);     break;
                case 2: BuildSensTab(parent);       break;
                case 3: BuildSaveTab(parent);       break;
            }
        }

        // 按鍵說明
        private void BuildKeyBindings(Transform parent)
        {
            float rowH = 1f / (KeyBindings.Length + 1);
            // 欄位標題
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

        // 存檔
        private void BuildSaveTab(Transform parent)
        {
            float rowH = 1f / SaveSystem.MaxSlots;
            for (int i = 0; i < SaveSystem.MaxSlots; i++)
            {
                int slot = i;
                float y  = 1f - rowH * (i + 0.5f);

                // 存檔摘要文字
                var lbl = new GameObject("Lbl", typeof(Text));
                lbl.transform.SetParent(parent, false);
                var t = lbl.GetComponent<Text>();
                t.font = _font; t.fontSize = 18;
                t.color = new Color(0.85f, 0.85f, 0.85f);
                t.alignment = TextAnchor.MiddleLeft;
                t.raycastTarget = false;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                var lrt = lbl.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0.02f, y - 0.07f);
                lrt.anchorMax = new Vector2(0.6f,  y + 0.07f);
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                _slotLabels[i] = t;

                // 存檔按鈕
                MakeSmallButton(parent, "存檔", new Vector2(0.65f, y), () => DoSave(slot));
            }
        }

        private void MakeSmallButton(Transform parent, string label, Vector2 anchor,
            System.Action onClick)
        {
            var go = new GameObject("Btn", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.15f, 0.4f, 0.15f, 0.9f);
            go.GetComponent<Button>().onClick.AddListener(() => onClick());
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(90f, 38f);

            var lbl = new GameObject("L", typeof(Text));
            lbl.transform.SetParent(go.transform, false);
            var t = lbl.GetComponent<Text>();
            t.font = _font; t.text = label; t.fontSize = 18;
            t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
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
