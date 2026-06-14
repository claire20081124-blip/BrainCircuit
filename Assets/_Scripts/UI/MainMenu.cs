using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;
using RunLight.Core;
using RunLight.Save;

namespace RunLight.UI
{
    /// <summary>
    /// 遊戲主選單(開始 / 繼續 / 遊戲選項 / 系統設定 / 離開)。
    ///
    /// 使用方式:在主選單場景建立一個空 GameObject,掛上本元件即可。
    /// 整個畫面(Canvas、EventSystem、標題、按鈕、設定面板)都在執行時用程式碼產生,
    /// 不需要手動在 Inspector 拉 UI;進 Play 模式就會出現。
    ///
    /// 之後若要換成美術做好的 TMP / 圖片版面,可把本檔當邏輯參考、改接設計師拉好的物件。
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("流程")]
        [Tooltip("按「開始遊戲」要載入的場景名稱(需加入 Build Settings)")]
        [SerializeField] private string gameSceneName = "SampleScene";

        [Tooltip("「開始遊戲」使用的存檔槽")]
        [SerializeField] private int newGameSlot = 0;

        [Header("文字")]
        [SerializeField] private string gameTitle = "溯光";
        [SerializeField] private string subtitle = "VERSO — Find the Light Within";

        // ---- 執行時建立的物件參考 ----
        private Button _continueButton;
        private GameObject _settingsPanel;

        // 主題色
        private static readonly Color BgTop = new(0.06f, 0.07f, 0.12f);
        private static readonly Color BgBottom = new(0.02f, 0.03f, 0.06f);
        private static readonly Color Accent = new(0.95f, 0.83f, 0.55f);   // 溫暖光色
        private static readonly Color TextColor = new(0.92f, 0.93f, 0.97f);

        private Font _font;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildUI();
        }

        private void Start() => RefreshContinueState();

        // ============================================================
        //  介面建構
        // ============================================================
        private void BuildUI()
        {
            // ---- Canvas ----
            var canvasGo = new GameObject("MainMenuCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // ---- 背景 ----
            var bg = CreateImage("Background", canvasGo.transform, BgBottom);
            Stretch(bg.rectTransform);
            // 上方再疊一層較亮的漸層感(用兩塊半透明色塊近似)
            var glow = CreateImage("Glow", canvasGo.transform, new Color(BgTop.r, BgTop.g, BgTop.b, 0.85f));
            var glowRt = glow.rectTransform;
            glowRt.anchorMin = new Vector2(0, 0.4f);
            glowRt.anchorMax = new Vector2(1, 1);
            glowRt.offsetMin = glowRt.offsetMax = Vector2.zero;

            // ---- 標題 ----
            var title = CreateText("Title", canvasGo.transform, gameTitle, 120, Accent, FontStyle.Bold);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0, -160);
            titleRt.sizeDelta = new Vector2(1200, 180);

            var sub = CreateText("Subtitle", canvasGo.transform, subtitle, 36, TextColor, FontStyle.Italic);
            var subRt = sub.rectTransform;
            subRt.anchorMin = new Vector2(0.5f, 1f);
            subRt.anchorMax = new Vector2(0.5f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0, -300);
            subRt.sizeDelta = new Vector2(1200, 60);

            // ---- 按鈕欄 ----
            var column = new GameObject("ButtonColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            column.transform.SetParent(canvasGo.transform, false);
            var colRt = column.GetComponent<RectTransform>();
            colRt.anchorMin = new Vector2(0.5f, 0.5f);
            colRt.anchorMax = new Vector2(0.5f, 0.5f);
            colRt.pivot = new Vector2(0.5f, 0.5f);
            colRt.anchoredPosition = new Vector2(0, -120);
            colRt.sizeDelta = new Vector2(440, 520);
            var vlg = column.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 22;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            CreateMenuButton(column.transform, "開始遊戲", OnStart);
            _continueButton = CreateMenuButton(column.transform, "繼續遊戲", OnContinue);
            CreateMenuButton(column.transform, "設定", () => Show(_settingsPanel, true));
            CreateMenuButton(column.transform, "離開遊戲", OnQuit);

            // ---- 版本字 ----
            var ver = CreateText("Version", canvasGo.transform, $"RunLight · v{Application.version}", 24,
                new Color(TextColor.r, TextColor.g, TextColor.b, 0.5f), FontStyle.Normal);
            var verRt = ver.rectTransform;
            verRt.anchorMin = new Vector2(1, 0);
            verRt.anchorMax = new Vector2(1, 0);
            verRt.pivot = new Vector2(1, 0);
            verRt.anchoredPosition = new Vector2(-30, 24);
            verRt.sizeDelta = new Vector2(400, 40);
            ver.alignment = TextAnchor.LowerRight;

            // ---- 設定面板(音訊 / 顯示 / 遊戲整合在一起)----
            _settingsPanel = BuildSettingsPanel(canvasGo.transform);
            Show(_settingsPanel, false);
        }

        // ---------- 設定面板 ----------
        private GameObject BuildSettingsPanel(Transform parent)
        {
            var (panel, content) = CreatePanelShell(parent, "設定");

            // 音訊
            float vol = PlayerPrefs.GetFloat(PrefMasterVolume, 1f);
            AudioListener.volume = vol;
            CreateSlider(content, "主音量", vol, v =>
            {
                AudioListener.volume = v;
                PlayerPrefs.SetFloat(PrefMasterVolume, v);
            });

            // 顯示
            CreateToggle(content, "全螢幕", Screen.fullScreen, on => Screen.fullScreen = on);

            // 遊戲
            CreateSlider(content, "文字速度", PlayerPrefs.GetFloat(PrefTextSpeed, 0.5f),
                v => PlayerPrefs.SetFloat(PrefTextSpeed, v));

            CreateToggle(content, "自動存檔", PlayerPrefs.GetInt(PrefAutoSave, 1) == 1,
                on => PlayerPrefs.SetInt(PrefAutoSave, on ? 1 : 0));

            CreateMenuButton(content, "返回", () => { PlayerPrefs.Save(); Show(panel, false); }, 320);
            return panel;
        }

        // ============================================================
        //  按鈕行為
        // ============================================================
        private void OnStart()
        {
            GameManager.Instance.NewGame(newGameSlot);
            LoadScene(gameSceneName);
        }

        private void OnContinue()
        {
            int slot = FindLatestSaveSlot();
            if (slot < 0)
            {
                Debug.LogWarning("[MainMenu] 沒有可讀取的存檔。");
                return;
            }

            if (!GameManager.Instance.LoadGame(slot))
            {
                Debug.LogError($"[MainMenu] 讀取槽 {slot} 失敗。");
                return;
            }

            // 優先回到存檔當下的場景,讀不到就退回預設遊戲場景。
            var data = SaveSystem.PeekSummary(slot);
            string scene = data != null && !string.IsNullOrEmpty(data.currentScene)
                ? data.currentScene
                : gameSceneName;
            LoadScene(scene);
        }

        private void OnQuit()
        {
            Debug.Log("[MainMenu] 離開遊戲。");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>掃描所有存檔槽,回傳存檔時間最新的一槽;都沒有則回傳 -1。</summary>
        private static int FindLatestSaveSlot()
        {
            int best = -1;
            string bestIso = null;
            for (int slot = 0; slot < SaveSystem.MaxSlots; slot++)
            {
                if (!SaveSystem.SlotExists(slot)) continue;
                var data = SaveSystem.PeekSummary(slot);
                if (data == null) continue;
                if (bestIso == null || string.CompareOrdinal(data.savedAtIso, bestIso) > 0)
                {
                    bestIso = data.savedAtIso;
                    best = slot;
                }
            }
            return best;
        }

        private void RefreshContinueState()
        {
            bool hasSave = FindLatestSaveSlot() >= 0;
            if (_continueButton != null) _continueButton.interactable = hasSave;
        }

        private void LoadScene(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"[MainMenu] 場景「{sceneName}」不在 Build Settings 中,無法載入。" +
                                 "請到 File ▸ Build Settings 把該場景加入清單。");
                return;
            }
            SceneManager.LoadScene(sceneName);
        }

        // ============================================================
        //  UI 工具方法
        // ============================================================
        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            // 只在本場景建立;切到遊戲場景時會隨選單一起卸載,由該場景自備 EventSystem。
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void Show(GameObject panel, bool visible)
        {
            if (panel != null) panel.SetActive(visible);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private Text CreateText(string name, Transform parent, string content,
            int size, Color color, FontStyle style)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>建立主選單樣式的按鈕,回傳 Button 以便外部調整(例如停用「繼續」)。</summary>
        private Button CreateMenuButton(Transform parent, string label, Action onClick, float width = 440)
        {
            var go = new GameObject($"Button_{label}", typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.06f);

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 72;
            le.minHeight = 72;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.06f);
            colors.highlightedColor = new Color(Accent.r, Accent.g, Accent.b, 0.22f);
            colors.pressedColor = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.02f);
            colors.fadeDuration = 0.12f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());

            var text = CreateText("Label", go.transform, label, 40, TextColor, FontStyle.Normal);
            Stretch(text.rectTransform);

            return btn;
        }

        /// <summary>建立半透明遮罩 + 置中對話框的面板殼,回傳 (面板根, 內容容器)。</summary>
        private (GameObject panel, Transform content) CreatePanelShell(Transform parent, string title)
        {
            // 全螢幕遮罩(同時擋住後方點擊)
            var panel = new GameObject($"Panel_{title}", typeof(Image));
            panel.transform.SetParent(parent, false);
            var mask = panel.GetComponent<Image>();
            mask.color = new Color(0, 0, 0, 0.7f);
            Stretch(panel.GetComponent<RectTransform>());

            // 對話框
            var box = new GameObject("Box", typeof(Image), typeof(VerticalLayoutGroup));
            box.transform.SetParent(panel.transform, false);
            box.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.18f, 0.98f);
            var boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = boxRt.anchorMax = boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(700, 800);
            var vlg = box.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(48, 48, 40, 40);
            vlg.spacing = 28;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var header = CreateText("Header", box.transform, title, 56, Accent, FontStyle.Bold);
            header.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 80);
            header.gameObject.AddComponent<LayoutElement>().minHeight = 80;

            return (panel, box.transform);
        }

        /// <summary>建立「標籤 + 滑桿」的一列。</summary>
        private void CreateSlider(Transform parent, string label, float value, Action<float> onChanged)
        {
            var row = new GameObject($"Row_{label}", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().minHeight = 90;

            var cap = CreateText("Cap", row.transform, label, 32, TextColor, FontStyle.Normal);
            var capRt = cap.rectTransform;
            capRt.anchorMin = new Vector2(0, 1);
            capRt.anchorMax = new Vector2(1, 1);
            capRt.pivot = new Vector2(0.5f, 1);
            capRt.offsetMin = new Vector2(0, -36);
            capRt.offsetMax = new Vector2(0, 0);
            cap.alignment = TextAnchor.MiddleLeft;

            // 滑桿
            var sliderGo = new GameObject("Slider", typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var sRt = sliderGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0, 0);
            sRt.anchorMax = new Vector2(1, 0);
            sRt.pivot = new Vector2(0.5f, 0);
            sRt.offsetMin = new Vector2(0, 8);
            sRt.offsetMax = new Vector2(0, 36);

            var bgImg = CreateImage("Background", sliderGo.transform, new Color(1, 1, 1, 0.12f));
            Stretch(bgImg.rectTransform);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            Stretch(faRt);
            var fill = CreateImage("Fill", fillArea.transform, Accent);
            fill.rectTransform.sizeDelta = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            Stretch(handleArea.GetComponent<RectTransform>());
            var handle = CreateImage("Handle", handleArea.transform, TextColor);
            handle.rectTransform.sizeDelta = new Vector2(28, 28);

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChanged(v));
        }

        /// <summary>建立「標籤 + 開關」的一列。</summary>
        private void CreateToggle(Transform parent, string label, bool value, Action<bool> onChanged)
        {
            var row = new GameObject($"Row_{label}", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().minHeight = 60;

            var cap = CreateText("Cap", row.transform, label, 32, TextColor, FontStyle.Normal);
            var capRt = cap.rectTransform;
            capRt.anchorMin = new Vector2(0, 0);
            capRt.anchorMax = new Vector2(0.7f, 1);
            capRt.offsetMin = capRt.offsetMax = Vector2.zero;
            cap.alignment = TextAnchor.MiddleLeft;

            var toggleGo = new GameObject("Toggle", typeof(Toggle));
            toggleGo.transform.SetParent(row.transform, false);
            var tRt = toggleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(1, 0.5f);
            tRt.anchorMax = new Vector2(1, 0.5f);
            tRt.pivot = new Vector2(1, 0.5f);
            tRt.anchoredPosition = Vector2.zero;
            tRt.sizeDelta = new Vector2(48, 48);

            var box = CreateImage("Box", toggleGo.transform, new Color(1, 1, 1, 0.12f));
            Stretch(box.rectTransform);
            var check = CreateImage("Check", box.transform, Accent);
            Stretch(check.rectTransform);
            check.rectTransform.offsetMin = new Vector2(8, 8);
            check.rectTransform.offsetMax = new Vector2(-8, -8);

            var toggle = toggleGo.GetComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = value;
            toggle.onValueChanged.AddListener(on => onChanged(on));
        }

        // PlayerPrefs 鍵名
        private const string PrefMasterVolume = "rl_master_volume";
        private const string PrefTextSpeed = "rl_text_speed";
        private const string PrefAutoSave = "rl_auto_save";
    }
}
