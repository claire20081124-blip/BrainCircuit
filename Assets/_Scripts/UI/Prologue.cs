using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace RunLight.UI
{
    [System.Serializable]
    public struct DialogueLine
    {
        [Tooltip("說話的角色名稱（留空則不顯示名牌）")]
        public string characterName;

        [Tooltip("對話內容")]
        [TextArea(2, 4)]
        public string text;
    }

    /// <summary>
    /// 遊戲開場：先播放 MP4 影片，影片結束後顯示 VN 風格對話框，全部完成後進入遊戲場景。
    ///
    /// 操作
    ///   點擊 / 空白 / Enter → 影片中跳過影片；打字中跳到行末；行已完推進下一行
    ///   ESC                 → 跳過全部，直接進入遊戲
    /// </summary>
    public class Prologue : MonoBehaviour
    {
        [Header("流程")]
        [SerializeField] private string nextSceneName = "SampleScene";

        [Header("開場影片（選填）")]
        [Tooltip("拖入 MP4；留空則略過影片直接進對話")]
        [SerializeField] private VideoClip openingVideo;
        [SerializeField] private float videoFadeIn    = 0.5f;
        [SerializeField] private float videoFadeOut   = 0.8f;
        [Tooltip("影片播放速度，1 = 正常，0.5 = 半速")]
        [SerializeField] private float playbackSpeed   = 1f;
        [Tooltip("影片總秒數（填影片實際長度，0 = 自動偵測）")]
        [SerializeField] private float videoDuration   = 0f;

        [Header("場景切換")]
        [Tooltip("對話結束後淡黑的秒數")]
        [SerializeField] private float sceneTransitionFade = 1.5f;
        [Tooltip("全黑後停留幾秒再載入場景")]
        [SerializeField] private float sceneTransitionHold = 0.5f;

        [Header("開場白文字（影片前）")]
        [SerializeField] private string[] openingTexts;
        [SerializeField] private float    textFadeIn    = 0.5f;
        [SerializeField] private float    textHold      = 2.0f;
        [SerializeField] private float    textFadeOut   = 0.8f;
        [SerializeField] private float    textCharDelay = 0.06f;
        [SerializeField] private int      textFontSize  = 36;

        [Header("對話（影片結束後，可留空）")]
        [SerializeField] private DialogueLine[] dialogueLines;
        [SerializeField] private float charInterval = 0.04f;

        // ── 色彩 ──────────────────────────────────────────────────────────────
        private static readonly Color DialogueBgColor = new(0.04f, 0.05f, 0.09f, 0.93f);
        private static readonly Color AccentColor     = new(0.95f, 0.83f, 0.55f);
        private static readonly Color TextColor       = new(0.92f, 0.93f, 0.97f);
        private static readonly Color HintColor       = new(0.60f, 0.62f, 0.68f, 0.55f);

        // ── UI 參照 ───────────────────────────────────────────────────────────
        private Image      _fadeOverlay;
        private RawImage   _videoDisplay;
        private Text       _openingTextLabel;
        private CanvasGroup _openingTextGroup;
        private GameObject _dialogueBox;
        private Text       _nameText;
        private Text       _dialogueText;
        private Text       _hintText;
        private Font       _font;

        // ── 狀態機 ────────────────────────────────────────────────────────────
        private enum Phase { Opening, Video, DialogueTyping, DialogueWait, Done }
        private Phase _phase      = Phase.Video;
        private bool  _skipVideo;
        private bool  _skipTyping;
        private bool  _inputSignal;

        // ── Input Actions ─────────────────────────────────────────────────────
        private InputAction _advanceAction;
        private InputAction _escapeAction;

        // ── 生命週期 ──────────────────────────────────────────────────────────
        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildUI();
            SetupInput();
        }

        private void SetupInput()
        {
            _advanceAction = new InputAction(type: InputActionType.Button);
            _advanceAction.AddBinding("<Mouse>/leftButton");
            _advanceAction.AddBinding("<Keyboard>/space");
            _advanceAction.AddBinding("<Keyboard>/enter");
            _advanceAction.AddBinding("<Keyboard>/numpadEnter");
            _advanceAction.performed += _ => { if (Time.timeSinceLevelLoad > 0.5f) AdvanceInput(); };
            _advanceAction.Enable();

            _escapeAction = new InputAction(type: InputActionType.Button);
            _escapeAction.AddBinding("<Keyboard>/escape");
            _escapeAction.performed += _ => { StopAllCoroutines(); StartCoroutine(FadeAndLoad()); };
            _escapeAction.Enable();
        }

        private void OnDestroy()
        {
            _advanceAction?.Disable();
            _advanceAction?.Dispose();
            _escapeAction?.Disable();
            _escapeAction?.Dispose();
        }

        private IEnumerator Start()
        {
            // 1. 開場白文字
            if (openingTexts != null && openingTexts.Length > 0)
                yield return RunOpeningTexts();

            // 2. 動畫影片
            if (openingVideo != null)
                yield return PlayVideo();

            // 3. 對話（選填）
            if (dialogueLines != null && dialogueLines.Length > 0)
                yield return RunDialogue();
            else
                yield return FadeAndLoad();
        }

        private void AdvanceInput()
        {
            switch (_phase)
            {
                case Phase.Opening:        /* 開場白不可跳過 */ break;
                case Phase.Video:          _skipVideo   = true; break;
                case Phase.DialogueTyping: _skipTyping  = true; break;
                case Phase.DialogueWait:   _inputSignal = true; break;
            }
        }

        // ── 開場白文字 ────────────────────────────────────────────────────────
        private IEnumerator RunOpeningTexts()
        {
            _phase = Phase.Opening;
            _fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            _openingTextGroup.gameObject.SetActive(true);

            foreach (var line in openingTexts)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                _openingTextLabel.text = "";
                yield return FadeGroup(_openingTextGroup, 0f, 1f, textFadeIn);

                // 打字機效果
                foreach (char c in line)
                {
                    _openingTextLabel.text += c;
                    yield return new WaitForSeconds(textCharDelay);
                }

                // 打完後停留
                float t = 0f;
                while (t < textHold)
                {
                    t += Time.deltaTime;
                    yield return null;
                }

                yield return FadeGroup(_openingTextGroup, 1f, 0f, textFadeOut);
            }

            _openingTextGroup.gameObject.SetActive(false);
        }

        private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
        {
            float t = 0f;
            group.alpha = from;
            while (t < duration)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }

        // ── 影片播放 ──────────────────────────────────────────────────────────
        private IEnumerator PlayVideo()
        {
            int rtW = openingVideo.width  > 0 ? Mathf.Min((int)openingVideo.width,  1920) : 1920;
            int rtH = openingVideo.height > 0 ? Mathf.Min((int)openingVideo.height, 1080) : 1080;
            var rt = new RenderTexture(rtW, rtH, 0, RenderTextureFormat.ARGB32);
            rt.Create();
            _videoDisplay.texture = rt;
            _videoDisplay.gameObject.SetActive(true);

            var vp = gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake     = false;
            vp.clip            = openingVideo;
            vp.renderMode      = VideoRenderMode.RenderTexture;
            vp.targetTexture   = rt;
            vp.audioOutputMode = VideoAudioOutputMode.Direct;
            vp.skipOnDrop      = false;

            bool prepared = false;
            vp.prepareCompleted += _ => prepared = true;
            vp.Prepare();

            yield return new WaitUntil(() => prepared);

            // 更新 RT 尺寸以符合實際影片
            if ((int)vp.width != rtW || (int)vp.height != rtH)
            {
                rt.Release();
                rt.width  = (int)vp.width;
                rt.height = (int)vp.height;
                rt.Create();
                vp.targetTexture      = rt;
                _videoDisplay.texture = rt;
            }

            vp.playbackSpeed = playbackSpeed;
            vp.Play();
            _phase     = Phase.Video;
            _skipVideo = false;
            yield return FadeOverlay(1f, 0f, videoFadeIn);

            float duration = videoDuration > 0 ? videoDuration / playbackSpeed
                           : (vp.length > 1   ? (float)(vp.length / playbackSpeed)
                                              : 10f);
            float elapsed = 0f;
            while (elapsed < duration && !_skipVideo)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return FadeOverlay(0f, 1f, videoFadeOut);
            vp.Stop();
            Destroy(vp);
            Destroy(rt);
            _videoDisplay.gameObject.SetActive(false);
        }

        // ── 對話系統 ──────────────────────────────────────────────────────────
        private IEnumerator RunDialogue()
        {
            _dialogueBox.SetActive(true);
            yield return FadeOverlay(1f, 0f, 0.5f);

            for (int i = 0; i < dialogueLines.Length; i++)
            {
                var  line   = dialogueLines[i];
                bool isLast = i == dialogueLines.Length - 1;

                _nameText.transform.parent.gameObject.SetActive(false);
                _dialogueText.text = "";
                _hintText.text     = "";

                // 打字機效果
                _phase      = Phase.DialogueTyping;
                _skipTyping = false;
                foreach (char c in line.text ?? "")
                {
                    if (_skipTyping) break;
                    _dialogueText.text += c;
                    yield return new WaitForSeconds(charInterval);
                }
                _dialogueText.text = line.text ?? "";

                // 等待玩家推進
                _hintText.text = isLast ? "── 按任意鍵繼續 ──" : "▼";
                _phase         = Phase.DialogueWait;
                _inputSignal   = false;
                yield return new WaitUntil(() => _inputSignal);
                _inputSignal   = false;
            }

            yield return FadeAndLoad();
        }

        // ── 切換場景 ──────────────────────────────────────────────────────────
        private IEnumerator FadeAndLoad()
        {
            _phase = Phase.Done;
            yield return FadeOverlay(0f, 1f, sceneTransitionFade);
            yield return new WaitForSeconds(sceneTransitionHold);
            LoadNext();
        }

        private void LoadNext()
        {
            try
            {
                SceneManager.LoadScene(nextSceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Prologue] 無法載入場景「{nextSceneName}」：{e.Message}");
            }
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
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            var bg = MakeImg("Background", canvasGo.transform, Color.black);
            Stretch(bg.rectTransform);
            bg.raycastTarget = false;

            var videoGo = new GameObject("VideoDisplay", typeof(RawImage));
            videoGo.transform.SetParent(canvasGo.transform, false);
            _videoDisplay = videoGo.GetComponent<RawImage>();
            _videoDisplay.raycastTarget = false;
            Stretch(_videoDisplay.rectTransform);
            videoGo.SetActive(false);

            // 開場白文字層
            var otGo = new GameObject("OpeningTextGroup", typeof(CanvasGroup), typeof(RectTransform));
            otGo.transform.SetParent(canvasGo.transform, false);
            _openingTextGroup = otGo.GetComponent<CanvasGroup>();
            _openingTextGroup.alpha = 0f;
            Stretch(otGo.GetComponent<RectTransform>());
            otGo.SetActive(false);

            _openingTextLabel = MakeTxt("OpeningText", otGo.transform, "", textFontSize,
                new Color(0.9f, 0.88f, 0.82f), FontStyle.Normal);
            _openingTextLabel.alignment        = TextAnchor.MiddleCenter;
            _openingTextLabel.lineSpacing      = 1.6f;
            _openingTextLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _openingTextLabel.verticalOverflow   = VerticalWrapMode.Overflow;
            var otRt = _openingTextLabel.rectTransform;
            otRt.anchorMin = new Vector2(0.15f, 0.3f);
            otRt.anchorMax = new Vector2(0.85f, 0.7f);
            otRt.offsetMin = otRt.offsetMax = Vector2.zero;

            _dialogueBox = BuildDialogueBox(canvasGo.transform);
            _dialogueBox.SetActive(false);

            var fadeGo = new GameObject("FadeOverlay", typeof(Image));
            fadeGo.transform.SetParent(canvasGo.transform, false);
            _fadeOverlay = fadeGo.GetComponent<Image>();
            _fadeOverlay.color        = Color.black;
            _fadeOverlay.raycastTarget = false;
            Stretch(_fadeOverlay.rectTransform);
        }

        private GameObject BuildDialogueBox(Transform parent)
        {
            // 全螢幕容器（無背景，純文字旁白風格）
            var box = new GameObject("DialogueBox", typeof(RectTransform));
            box.transform.SetParent(parent, false);
            var boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = Vector2.zero;
            boxRt.anchorMax = Vector2.one;
            boxRt.offsetMin = boxRt.offsetMax = Vector2.zero;

            // 名字（旁白模式下通常留空，保留欄位供未來使用）
            _nameText = MakeTxt("NameText", box.transform, "", 32,
                AccentColor, FontStyle.Bold);
            var ntRt = _nameText.rectTransform;
            ntRt.anchorMin        = new Vector2(0.5f, 0.5f);
            ntRt.anchorMax        = new Vector2(0.5f, 0.5f);
            ntRt.pivot            = new Vector2(0.5f, 0f);
            ntRt.anchoredPosition = new Vector2(0f, 30f);
            ntRt.sizeDelta        = new Vector2(1200f, 50f);
            _nameText.alignment   = TextAnchor.MiddleCenter;

            // 主文字：置中，字體稍大
            _dialogueText = MakeTxt("DialogueText", box.transform, "", 48, TextColor, FontStyle.Normal);
            var dtRt = _dialogueText.rectTransform;
            dtRt.anchorMin = new Vector2(0.5f, 0.5f);
            dtRt.anchorMax = new Vector2(0.5f, 0.5f);
            dtRt.pivot     = new Vector2(0.5f, 0.5f);
            dtRt.anchoredPosition = Vector2.zero;
            dtRt.sizeDelta        = new Vector2(1200f, 400f);
            _dialogueText.alignment          = TextAnchor.MiddleCenter;
            _dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _dialogueText.verticalOverflow   = VerticalWrapMode.Overflow;

            // 提示文字：畫面底部中央
            _hintText = MakeTxt("HintText", box.transform, "", 26, HintColor, FontStyle.Italic);
            var htRt = _hintText.rectTransform;
            htRt.anchorMin        = new Vector2(0.5f, 0f);
            htRt.anchorMax        = new Vector2(0.5f, 0f);
            htRt.pivot            = new Vector2(0.5f, 0f);
            htRt.anchoredPosition = new Vector2(0f, 40f);
            htRt.sizeDelta        = new Vector2(600f, 40f);
            _hintText.alignment   = TextAnchor.MiddleCenter;

            return box;
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
            var go  = new GameObject(name, typeof(Image));
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
            t.font          = _font;
            t.text          = content;
            t.fontSize      = size;
            t.color         = color;
            t.fontStyle     = style;
            t.raycastTarget = false;
            return t;
        }
    }
}
