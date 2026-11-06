using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

namespace RunLight.Core
{
    public class IntroPlayer : MonoBehaviour
    {
        [System.Serializable]
        public struct TextEntry
        {
            [TextArea(2, 5)]
            public string text;
            [Tooltip("文字停留秒數")]
            public float holdTime;
        }

        [Header("開場白文字")]
        [SerializeField] private TextEntry[] introTexts;
        [SerializeField] private float fadeInTime  = 1.2f;
        [SerializeField] private float fadeOutTime = 0.8f;
        [SerializeField] private int   fontSize    = 32;
        [SerializeField] private Color textColor   = new Color(0.9f, 0.88f, 0.82f);

        [Header("影片（留空則跳過）")]
        [SerializeField] private VideoPlayer videoPlayer;

        [Header("下一個場景")]
        [SerializeField] private string nextScene = "場景";

        private CanvasGroup _textGroup;
        private Text        _label;
        private bool        _skipped;

        private void Awake()
        {
            BuildUI();
        }

        private void Start()
        {
            StartCoroutine(IntroSequence());
        }

        private void Update()
        {
            if (Input.anyKeyDown) _skipped = true;
        }

        private IEnumerator IntroSequence()
        {
            // ── 文字段落 ──────────────────────────────────────────
            foreach (var entry in introTexts)
            {
                if (_skipped) break;

                _label.text  = entry.text;
                _textGroup.gameObject.SetActive(true);

                yield return Fade(_textGroup, 0f, 1f, fadeInTime);

                float t = 0f;
                while (t < entry.holdTime && !_skipped)
                {
                    t += Time.deltaTime;
                    yield return null;
                }

                yield return Fade(_textGroup, 1f, 0f, fadeOutTime);
                _textGroup.gameObject.SetActive(false);
            }

            // ── 影片 ──────────────────────────────────────────────
            if (videoPlayer != null && videoPlayer.clip != null && !_skipped)
            {
                videoPlayer.gameObject.SetActive(true);
                videoPlayer.Play();

                bool ended = false;
                videoPlayer.loopPointReached += _ => ended = true;

                while (!ended && !_skipped)
                    yield return null;

                videoPlayer.Stop();
                videoPlayer.gameObject.SetActive(false);
            }

            // ── 進場景 ────────────────────────────────────────────
            SceneManager.LoadScene(nextScene);
        }

        private IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
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

        private void BuildUI()
        {
            var canvasGo = new GameObject("IntroCanvas",
                typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            // 黑底
            var bg = new GameObject("BG", typeof(Image));
            bg.transform.SetParent(canvasGo.transform, false);
            bg.GetComponent<Image>().color = Color.black;
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            // 文字 CanvasGroup
            var textGo = new GameObject("TextGroup", typeof(CanvasGroup));
            textGo.transform.SetParent(canvasGo.transform, false);
            _textGroup = textGo.GetComponent<CanvasGroup>();
            _textGroup.alpha = 0f;
            var tgRt = textGo.AddComponent<RectTransform>();
            tgRt.anchorMin = Vector2.zero; tgRt.anchorMax = Vector2.one;
            tgRt.offsetMin = tgRt.offsetMax = Vector2.zero;
            textGo.SetActive(false);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var lblGo = new GameObject("Label", typeof(Text));
            lblGo.transform.SetParent(textGo.transform, false);
            _label = lblGo.GetComponent<Text>();
            _label.font      = font;
            _label.fontSize  = fontSize;
            _label.color     = textColor;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.lineSpacing      = 1.5f;
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow   = VerticalWrapMode.Overflow;
            _label.raycastTarget = false;
            var lRt = lblGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0.15f, 0.3f);
            lRt.anchorMax = new Vector2(0.85f, 0.7f);
            lRt.offsetMin = lRt.offsetMax = Vector2.zero;
        }
    }
}
