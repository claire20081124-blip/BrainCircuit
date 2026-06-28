using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace RunLight.UI
{
    /// <summary>
    /// 遊戲開場旁白。
    ///
    /// 使用方式：建立「Prologue」場景，新增空 GameObject，掛上本元件即可。
    /// 整個畫面（Canvas、打字機文字、淡入遮罩）全由程式碼產生，不需手動佈局。
    ///
    /// 操作說明
    ///   點擊 / 空白鍵 / Enter → 打字中：跳到本行末；本行已完：推進下一行；全部結束：進入遊戲
    ///   ESC                   → 跳過全部旁白，直接進入遊戲
    /// </summary>
    public class Prologue : MonoBehaviour
    {
        [Header("流程")]
        [Tooltip("旁白結束後要載入的場景名稱（需加入 Build Settings）")]
        [SerializeField] private string nextSceneName = "SampleScene";

        [Header("速度")]
        [Tooltip("每個字元之間的間隔秒數")]
        [SerializeField] private float charInterval = 0.045f;

        [Tooltip("空行停頓秒數（可按鍵提前跳過）")]
        [SerializeField] private float pauseDuration = 0.85f;

        // ── 旁白台詞 ─────────────────────────────────────────────────────────
        // 空字串 = 視覺停頓；最後一行 = 標題顯示（自動放大 + 強調色）
        private static readonly string[] Lines =
        {
            "每個人的腦海深處，都有一座看不見的迷宮。",
            "由記憶堆砌的走廊，以情緒鋪就的迴路。",
            "",
            "大多數人，一輩子都不會走進去。",
            "他們在門口貼上「勿近」，然後繼續過日子。",
            "",
            "他叫簡程熠，二十六歲，左臉有一塊胎記。",
            "那天下午三點二十七分，他在辦公桌前倒下了。",
            "",
            "不是因為生病。",
            "是因為他的迴路，已經斷線太久了。",
            "",
            "意識陷入一片黑暗。",
            "然後，他聽見了——",
            "",
            "「你還記得，你是誰嗎？」",
            "",
            "這裡是別人從未踏入的地方。",
            "也是他唯一能夠找回自己的地方。",
            "",
            "腦　迴　路",
        };

        // ── 色彩 ──────────────────────────────────────────────────────────────
        private static readonly Color BgColor      = new(0.02f, 0.02f, 0.04f);
        private static readonly Color NarTextColor = new(0.92f, 0.93f, 0.97f);
        private static readonly Color AccentColor  = new(0.95f, 0.83f, 0.55f);
        private static readonly Color HintColor    = new(0.60f, 0.62f, 0.68f, 0.55f);

        // ── 執行時參照 ────────────────────────────────────────────────────────
        private Text  _mainText;
        private Text  _hintText;
        private Image _fadeOverlay;
        private Font  _font;

        private enum Phase { Typing, WaitClick, Pause, Done }
        private Phase _phase = Phase.Pause;
        private bool  _skipTyping;
        private bool  _inputSignal;

        // ── 生命週期 ──────────────────────────────────────────────────────────
        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildUI();
        }

        private IEnumerator Start()
        {
            yield return FadeOverlay(1f, 0f, 0.9f);
            yield return RunNarration();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StopAllCoroutines();
                StartCoroutine(FadeAndLoad());
                return;
            }

            bool pressed = Input.GetMouseButtonDown(0)
                        || Input.GetKeyDown(KeyCode.Space)
                        || Input.GetKeyDown(KeyCode.Return);
            if (!pressed) return;

            switch (_phase)
            {
                case Phase.Typing:    _skipTyping  = true; break;
                case Phase.WaitClick:
                case Phase.Pause:
                case Phase.Done:      _inputSignal = true; break;
            }
        }

        // ── 旁白核心流程 ──────────────────────────────────────────────────────
        private IEnumerator RunNarration()
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                string line   = Lines[i];
                bool   isLast = i == Lines.Length - 1;

                // 空行 → 視覺停頓，可按鍵提前跳過
                if (string.IsNullOrWhiteSpace(line))
                {
                    _mainText.text = "";
                    _hintText.text = "";
                    _phase = Phase.Pause;
                    _inputSignal = false;
                    float end = Time.time + pauseDuration;
                    yield return new WaitUntil(() => Time.time >= end || _inputSignal);
                    _inputSignal = false;
                    continue;
                }

                // 最後一行（標題）→ 放大 + 強調色
                _mainText.fontSize = isLast ? 80 : 44;
                _mainText.color    = isLast ? AccentColor : NarTextColor;
                _mainText.fontStyle = isLast ? FontStyle.Bold : FontStyle.Normal;

                // 打字機效果
                _phase      = Phase.Typing;
                _skipTyping = false;
                _mainText.text = "";
                foreach (char c in line)
                {
                    if (_skipTyping) break;
                    _mainText.text += c;
                    yield return new WaitForSeconds(charInterval);
                }
                _mainText.text = line; // 補全剩餘字元

                if (isLast)
                {
                    yield return new WaitForSeconds(0.8f); // 標題稍停後才顯示提示
                    _hintText.text = "── 按任意鍵繼續 ──";
                    _phase = Phase.Done;
                    _inputSignal = false;
                    yield return new WaitUntil(() => _inputSignal);
                    StartCoroutine(FadeAndLoad());
                    yield break;
                }

                // 等待玩家推進（顯示下箭頭提示）
                _hintText.text = "▼";
                _phase = Phase.WaitClick;
                _inputSignal = false;
                yield return new WaitUntil(() => _inputSignal);
                _hintText.text = "";
                _inputSignal = false;
            }
        }

        // ── 切換場景 ──────────────────────────────────────────────────────────
        private IEnumerator FadeAndLoad()
        {
            _phase = Phase.Done;
            _mainText.text = "";
            _hintText.text = "";
            yield return FadeOverlay(0f, 1f, 0.65f);

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogWarning($"[Prologue] 場景「{nextSceneName}」不在 Build Settings，請先加入。");
                yield break;
            }
            SceneManager.LoadScene(nextSceneName);
        }

        // ── 淡入 / 淡出 ───────────────────────────────────────────────────────
        private IEnumerator FadeOverlay(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            _fadeOverlay.color = new Color(0f, 0f, 0f, to);
        }

        // ── UI 建構 ───────────────────────────────────────────────────────────
        private void BuildUI()
        {
            var canvasGo = new GameObject("PrologueCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 純黑背景
            var bg = MakeImg("Background", canvasGo.transform, BgColor);
            Stretch(bg.rectTransform);
            bg.raycastTarget = false;

            // 主旁白文字（畫面中央偏上）
            _mainText = MakeTxt("NarratorText", canvasGo.transform, "", 44, NarTextColor, FontStyle.Normal);
            var mt = _mainText.rectTransform;
            mt.anchorMin = new Vector2(0.08f, 0.28f);
            mt.anchorMax = new Vector2(0.92f, 0.72f);
            mt.offsetMin = mt.offsetMax = Vector2.zero;
            _mainText.alignment          = TextAnchor.MiddleCenter;
            _mainText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _mainText.verticalOverflow   = VerticalWrapMode.Overflow;
            _mainText.raycastTarget      = false;

            // 提示文字（底部中央）
            _hintText = MakeTxt("HintText", canvasGo.transform, "", 30, HintColor, FontStyle.Italic);
            var ht = _hintText.rectTransform;
            ht.anchorMin        = new Vector2(0f, 0f);
            ht.anchorMax        = new Vector2(1f, 0f);
            ht.pivot            = new Vector2(0.5f, 0f);
            ht.anchoredPosition = new Vector2(0f, 52f);
            ht.sizeDelta        = new Vector2(0f, 48f);
            _hintText.alignment     = TextAnchor.MiddleCenter;
            _hintText.raycastTarget = false;

            // 淡入 / 淡出遮罩（最後渲染，初始全黑）
            _fadeOverlay = MakeImg("FadeOverlay", canvasGo.transform, Color.black);
            Stretch(_fadeOverlay.rectTransform);
            _fadeOverlay.raycastTarget = false;
            _fadeOverlay.transform.SetAsLastSibling();
        }

        // ── 工具方法 ──────────────────────────────────────────────────────────
        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private Image MakeImg(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
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
            return t;
        }
    }
}
