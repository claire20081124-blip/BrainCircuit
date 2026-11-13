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

        [Header("流程")]
        [SerializeField] private string mainMenuSceneName = "1";

        [Header("外觀")]
        [SerializeField] private string backgroundSpriteName = "ee";

        [Header("操作說明圖（有圖時取代文字表格）")]
        [SerializeField] private Sprite controlsGuideSprite;

        [Header("標題圖片（選填，不填用文字）")]
        [SerializeField] private Sprite titlePauseSprite;
        [SerializeField] private Sprite titleControlsSprite;

        [Header("按鈕圖片（選填，不填用純色）")]
        [SerializeField] private Sprite btnResumeSprite;
        [SerializeField] private Sprite btnSettingsSprite;
        [SerializeField] private Sprite btnQuitSprite;
        [SerializeField] private Sprite btnVolumeTabSprite;
        [SerializeField] private Sprite btnSensTabSprite;
        [SerializeField] private Sprite btnBackSprite;

        private GameObject _pauseCanvas;
        private GameObject _pausePanel;
        private GameObject _settingsPanel;
        private GameObject[] _tabContents; // unused after tab removal, kept for safety
        private Font        _font;
        private FirstPersonController _fpc;

        private static readonly string[] TabNames = { "音量", "靈敏度" };
        private static readonly string[] TabIcons = { "🔊", "🎯" };

        private static readonly (string key, string action)[] KeyBindings =
        {
            ("W A S D", "移動"),
            ("E",        "互動 / 確定"),
            ("Q",        "快速丟出"),
            ("Shift",    "加速"),
            ("Tab",      "背包"),
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

        private void OnDestroy()
        {
            if (IsPaused)
            {
                IsPaused = false;
                Time.timeScale = 1f;
            }
        }

        // ── 暫停控制 ──────────────────────────────────────────────

        private void TogglePause()
        {
            if (Interaction.BoxHideSystem.IsHiding) return;
            if (IsPaused) Resume(); else Pause();
        }

        private void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            _pauseCanvas.SetActive(true);
            _pausePanel.SetActive(true);
            _settingsPanel.SetActive(false);
            if (_fpc != null) _fpc.MovementLocked = true;
        }

        private void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            _pauseCanvas.SetActive(false);
            _pausePanel.SetActive(false);
            _settingsPanel.SetActive(false);
            if (_fpc != null) _fpc.MovementLocked = false;
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
            var gm  = GameManager.Instance;
            var fpc = _fpc != null ? _fpc : FindObjectOfType<FirstPersonController>();
            if (fpc != null)
                gm.SaveGame(0, fpc.transform.position, fpc.transform.eulerAngles.y);
            else
                gm.SaveGame(0);

            IsPaused = false;
            Time.timeScale = 1f;
            SceneFader.Instance.FadeOutAndLoad(mainMenuSceneName);
        }

        // ── Tab 切換 ──────────────────────────────────────────────

        private void ShowTab(int index)
        {
            if (_tabContents == null) return;
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
            // 半透明遮罩
            var panel = new GameObject("PausePanel", typeof(Image));
            panel.transform.SetParent(root, false);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // ── 左側卡片：按鈕 ──
            var leftCard = MakeCard(panel.transform,
                new Vector2(0.27f, 0.18f), new Vector2(0.50f, 0.84f));

            // 頂部藍色強調線
            MakeAccent(leftCard.transform);

            if (titlePauseSprite != null)
            {
                var tgo = new GameObject("TitleImg", typeof(Image));
                tgo.transform.SetParent(leftCard.transform, false);
                var timg = tgo.GetComponent<Image>();
                timg.sprite = titlePauseSprite; timg.type = Image.Type.Simple;
                timg.preserveAspect = true; timg.raycastTarget = false;
                var trt = tgo.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.15f, 0.78f); trt.anchorMax = new Vector2(0.85f, 0.90f);
                trt.offsetMin = trt.offsetMax = Vector2.zero;
            }
            else
            {
                MakeLabel(leftCard.transform, "暫停", 18, Color.white, new Vector2(0.5f, 0.82f), new Vector2(220f, 28f));
            }

            string[] labels  = { "繼續", "設定", "離開遊戲" };
            float[]  yPcts   = { 0.62f, 0.44f, 0.24f };
            System.Action[] actions = { Resume, OpenSettings, QuitGame };
            Sprite[] btnSprites = { btnResumeSprite, btnSettingsSprite, btnQuitSprite };

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                MakePanelButton(leftCard.transform, labels[i], yPcts[i],
                    new Color(0.14f, 0.17f, 0.24f, 0.98f), actions[idx], 0.80f, btnSprites[idx]);
            }

            // ── 右側卡片：按鍵說明 ──
            var rightCard = MakeCard(panel.transform,
                new Vector2(0.52f, 0.18f), new Vector2(0.75f, 0.84f));
            MakeAccent(rightCard.transform);

            if (titleControlsSprite != null)
            {
                var tgo = new GameObject("TitleImg", typeof(Image));
                tgo.transform.SetParent(rightCard.transform, false);
                var timg = tgo.GetComponent<Image>();
                timg.sprite = titleControlsSprite; timg.type = Image.Type.Simple;
                timg.preserveAspect = true; timg.raycastTarget = false;
                var trt = tgo.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.1f, 0.79f); trt.anchorMax = new Vector2(0.9f, 0.91f);
                trt.offsetMin = trt.offsetMax = Vector2.zero;
            }
            else
            {
                MakeLabel(rightCard.transform, "按鍵說明", 14, new Color(0.7f,0.88f,1f), new Vector2(0.5f, 0.82f), new Vector2(220f, 28f));
            }

            if (controlsGuideSprite != null)
            {
                var imgGo = new GameObject("Guide", typeof(Image));
                imgGo.transform.SetParent(rightCard.transform, false);
                var img = imgGo.GetComponent<Image>();
                img.sprite = controlsGuideSprite;
                img.preserveAspect = true;
                img.raycastTarget  = false;
                var irt = imgGo.GetComponent<RectTransform>();
                irt.anchorMin = new Vector2(0.04f, 0.06f);
                irt.anchorMax = new Vector2(0.96f, 0.80f);
                irt.offsetMin = irt.offsetMax = Vector2.zero;
            }
            else
            {
                float rowH = 0.68f / KeyBindings.Length;
                for (int i = 0; i < KeyBindings.Length; i++)
                {
                    float y = 0.76f - rowH * i - rowH * 0.5f;

                    // 按鍵底色
                    var kbg = new GameObject("KBg" + i, typeof(Image));
                    kbg.transform.SetParent(rightCard.transform, false);
                    kbg.GetComponent<Image>().color = new Color(0.18f, 0.20f, 0.28f, 1f);
                    kbg.GetComponent<Image>().raycastTarget = false;
                    var kbrt = kbg.GetComponent<RectTransform>();
                    kbrt.anchorMin = new Vector2(0.04f, y - rowH * 0.36f);
                    kbrt.anchorMax = new Vector2(0.44f, y + rowH * 0.36f);
                    kbrt.offsetMin = kbrt.offsetMax = Vector2.zero;

                    // 按鍵文字
                    var kt = new GameObject("K" + i, typeof(Text));
                    kt.transform.SetParent(rightCard.transform, false);
                    var ktext = kt.GetComponent<Text>();
                    ktext.font = _font; ktext.text = KeyBindings[i].key; ktext.fontSize = 13;
                    ktext.color = new Color(0.65f, 0.88f, 1f);
                    ktext.alignment = TextAnchor.MiddleCenter;
                    ktext.raycastTarget = false;
                    ktext.horizontalOverflow = HorizontalWrapMode.Overflow;
                    var ktrt = kt.GetComponent<RectTransform>();
                    ktrt.anchorMin = new Vector2(0.04f, y - rowH * 0.36f);
                    ktrt.anchorMax = new Vector2(0.44f, y + rowH * 0.36f);
                    ktrt.offsetMin = ktrt.offsetMax = Vector2.zero;

                    // 動作文字
                    var at = new GameObject("A" + i, typeof(Text));
                    at.transform.SetParent(rightCard.transform, false);
                    var atext = at.GetComponent<Text>();
                    atext.font = _font; atext.text = KeyBindings[i].action; atext.fontSize = 13;
                    atext.color = new Color(0.88f, 0.90f, 0.95f);
                    atext.alignment = TextAnchor.MiddleLeft;
                    atext.raycastTarget = false;
                    var atrt = at.GetComponent<RectTransform>();
                    atrt.anchorMin = new Vector2(0.48f, y - rowH * 0.36f);
                    atrt.anchorMax = new Vector2(0.98f, y + rowH * 0.36f);
                    atrt.offsetMin = new Vector2(6f, 0f); atrt.offsetMax = Vector2.zero;
                }
            }

            return panel;
        }

        private GameObject MakeCard(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Card", typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.96f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        private void MakeAccent(Transform parent)
        {
            var go = new GameObject("Accent", typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.35f, 0.65f, 1f, 0.9f);
            go.GetComponent<Image>().raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, 3f);
        }

        // ── 設定面板 ──────────────────────────────────────────────

        private GameObject BuildSettingsPanel(Transform root)
        {
            var panel = MakePanel(root, new Vector2(960f, 640f));

            // 內容區（全寬，留底部給返回按鈕）
            var contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(panel.transform, false);
            var caRt = contentArea.AddComponent<RectTransform>();
            caRt.anchorMin = new Vector2(0.04f, 0.18f);
            caRt.anchorMax = new Vector2(0.96f, 0.92f);
            caRt.offsetMin = caRt.offsetMax = Vector2.zero;

            // 直接顯示兩條 slider
            BuildVolumeTab(contentArea.transform, 0.72f);
            BuildSensTab(contentArea.transform, 0.38f);

            // 返回按鈕（左下）
            var backBtn = new GameObject("Back", typeof(Image), typeof(Button));
            backBtn.transform.SetParent(panel.transform, false);
            var backImg = backBtn.GetComponent<Image>();
            if (btnBackSprite != null)
            { backImg.sprite = btnBackSprite; backImg.type = Image.Type.Simple; backImg.color = Color.white; backImg.preserveAspect = true; }
            else
            { backImg.color = new Color(0.3f, 0.12f, 0.12f, 0.9f); }
            backBtn.GetComponent<Button>().onClick.AddListener(BackToPause);
            var brt = backBtn.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.04f, 0.02f); brt.anchorMax = new Vector2(0.22f, 0.12f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            if (btnBackSprite == null)
            {
                var blbl = new GameObject("L", typeof(Text));
                blbl.transform.SetParent(backBtn.transform, false);
                var bt = blbl.GetComponent<Text>();
                bt.font = _font; bt.text = "← 返回"; bt.fontSize = 18;
                bt.color = Color.white; bt.alignment = TextAnchor.MiddleCenter;
                bt.raycastTarget = false;
                var blrt = blbl.GetComponent<RectTransform>();
                blrt.anchorMin = Vector2.zero; blrt.anchorMax = Vector2.one;
                blrt.offsetMin = blrt.offsetMax = Vector2.zero;
            }

            return panel;
        }

        private void BuildTabContent(Transform parent, int tab)
        {
            switch (tab)
            {
                case 0: BuildVolumeTab(parent); break;
                case 1: BuildSensTab(parent);   break;
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
        private void BuildVolumeTab(Transform parent, float yAnchor = 0.65f)
        {
            MakeSliderRow(parent, "主音量", yAnchor, 0f, 1f, AudioListener.volume, OnVolumeChanged);
        }

        // 靈敏度
        private void BuildSensTab(Transform parent, float yAnchor = 0.65f)
        {
            float cur = FindObjectOfType<FirstPersonController>()?.GetSensitivity() ?? 0.15f;
            MakeSliderRow(parent, "滑鼠靈敏度", yAnchor, 0.03f, 0.5f, cur, OnSensitivityChanged);
        }


        private void MakeSliderRow(Transform parent, string label, float yAnchor,
            float min, float max, float current,
            UnityEngine.Events.UnityAction<float> onChange)
        {
            // 文字標籤
            var lbl = new GameObject("Lbl", typeof(Text));
            lbl.transform.SetParent(parent, false);
            var lt = lbl.GetComponent<Text>();
            lt.font = _font; lt.text = label; lt.fontSize = 28;
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
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.97f);
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
            Color bg, System.Action onClick, float width = 0.7f, Sprite sprite = null)
        {
            var go = new GameObject("Btn", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var btnImg = go.GetComponent<Image>();
            if (sprite != null)
            { btnImg.sprite = sprite; btnImg.type = Image.Type.Simple; btnImg.color = Color.white; btnImg.preserveAspect = true; }
            else
            { btnImg.color = bg; }
            go.GetComponent<Button>().onClick.AddListener(() => onClick());
            var rt = go.GetComponent<RectTransform>();
            float margin = (1f - width) / 2f;
            rt.anchorMin = new Vector2(margin, yAnchor - 0.07f);
            rt.anchorMax = new Vector2(1f - margin, yAnchor + 0.07f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            if (sprite == null)
            {
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
