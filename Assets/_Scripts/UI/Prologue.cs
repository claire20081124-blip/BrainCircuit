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
        [SerializeField] private float videoFadeIn  = 0.5f;
        [SerializeField] private float videoFadeOut = 0.8f;

        [Header("對話（影片結束後）")]
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
        private GameObject _dialogueBox;
        private Text       _nameText;
        private Text       _dialogueText;
        private Text       _hintText;
        private Font       _font;

        // ── 狀態機 ────────────────────────────────────────────────────────────
        private enum Phase { Video, DialogueTyping, DialogueWait, Done }
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
            _advanceAction.performed += _ => AdvanceInput();
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
            if (openingVideo != null)
                yield return PlayVideo();

            if (dialogueLines != null && dialogueLines.Length > 0)
                yield return RunDialogue();
            else
                LoadNext();
        }

        private void AdvanceInput()
        {
            switch (_phase)
            {
                case Phase.Video:          _skipVideo   = true; break;
                case Phase.DialogueTyping: _skipTyping  = true; break;
                case Phase.DialogueWait:   _inputSignal = true; break;
            }
        }

        // ── 影片播放 ──────────────────────────────────────────────────────────
        private IEnumerator PlayVideo()
        {
            var rt = new RenderTexture(1920, 1080, 0);
            _videoDisplay.texture = rt;
            _videoDisplay.gameObject.SetActive(true);

            var vp = gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake     = false;
            vp.clip            = openingVideo;
            vp.renderMode      = VideoRenderMode.RenderTexture;
            vp.targetTexture   = rt;
            vp.audioOutputMode = VideoAudioOutputMode.Direct;
            vp.Prepare();

            yield return new WaitUntil(() => vp.isPrepared);

            vp.Play();
            _phase     = Phase.Video;
            _skipVideo = false;
            yield return FadeOverlay(1f, 0f, videoFadeIn);

            yield return new WaitUntil(() => !vp.isPlaying || _skipVideo);

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

                _nameText.transform.parent.gameObject.SetActive(
                    !string.IsNullOrWhiteSpace(line.characterName));
                _nameText.text     = line.characterName ?? "";
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
            yield return FadeOverlay(0f, 1f, 0.65f);

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogWarning($"[Prologue] 場景「{nextSceneName}」不在 Build Settings，請先加入。");
                yield break;
            }
            SceneManager.LoadScene(nextSceneName);
        }

        private void LoadNext()
        {
            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogWarning($"[Prologue] 場景「{nextSceneName}」不在 Build Settings，請先加入。");
                return;
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
            var box = new GameObject("DialogueBox", typeof(Image));
            box.transform.SetParent(parent, false);
            var boxImg = box.GetComponent<Image>();
            boxImg.color        = DialogueBgColor;
            boxImg.raycastTarget = false;
            var boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0f, 0f);
            boxRt.anchorMax = new Vector2(1f, 0.28f);
            boxRt.offsetMin = boxRt.offsetMax = Vector2.zero;

            var nameTag = new GameObject("NameTag", typeof(Image));
            nameTag.transform.SetParent(box.transform, false);
            var ntImg = nameTag.GetComponent<Image>();
            ntImg.color        = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.95f);
            ntImg.raycastTarget = false;
            var ntRt = nameTag.GetComponent<RectTransform>();
            ntRt.anchorMin        = new Vector2(0f, 1f);
            ntRt.anchorMax        = new Vector2(0f, 1f);
            ntRt.pivot            = new Vector2(0f, 0f);
            ntRt.anchoredPosition = new Vector2(48f, 0f);
            ntRt.sizeDelta        = new Vector2(240f, 56f);

            _nameText = MakeTxt("NameText", nameTag.transform, "", 34,
                new Color(0.08f, 0.08f, 0.10f), FontStyle.Bold);
            Stretch(_nameText.rectTransform);
            _nameText.alignment = TextAnchor.MiddleCenter;

            _dialogueText = MakeTxt("DialogueText", box.transform, "", 38, TextColor, FontStyle.Normal);
            var dtRt = _dialogueText.rectTransform;
            dtRt.anchorMin = new Vector2(0f, 0f);
            dtRt.anchorMax = new Vector2(1f, 1f);
            dtRt.offsetMin = new Vector2(60f, 24f);
            dtRt.offsetMax = new Vector2(-60f, -16f);
            _dialogueText.alignment          = TextAnchor.UpperLeft;
            _dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _dialogueText.verticalOverflow   = VerticalWrapMode.Overflow;

            _hintText = MakeTxt("HintText", box.transform, "", 26, HintColor, FontStyle.Italic);
            var htRt = _hintText.rectTransform;
            htRt.anchorMin        = new Vector2(1f, 0f);
            htRt.anchorMax        = new Vector2(1f, 0f);
            htRt.pivot            = new Vector2(1f, 0f);
            htRt.anchoredPosition = new Vector2(-40f, 18f);
            htRt.sizeDelta        = new Vector2(320f, 40f);
            _hintText.alignment   = TextAnchor.MiddleRight;

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
