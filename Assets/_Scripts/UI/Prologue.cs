using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace RunLight.UI
{
    /// <summary>
    /// 遊戲開場：播放 MP4 影片，播完後自動進入遊戲場景。
    ///
    /// 使用方式：建立「Prologue」場景，新增空 GameObject，掛上本元件即可。
    /// Opening Video 留空 → 略過影片，直接載入遊戲場景。
    ///
    /// 操作說明
    ///   點擊 / 空白鍵 / Enter / ESC → 跳過影片，直接進入遊戲
    /// </summary>
    public class Prologue : MonoBehaviour
    {
        [Header("流程")]
        [Tooltip("影片結束後要載入的場景名稱（需加入 Build Settings）")]
        [SerializeField] private string nextSceneName = "SampleScene";

        [Header("開場影片（選填）")]
        [Tooltip("拖入 MP4 影片 Asset；留空則直接進入遊戲場景")]
        [SerializeField] private VideoClip openingVideo;

        [Tooltip("影片淡入秒數")]
        [SerializeField] private float videoFadeIn = 0.5f;

        [Tooltip("影片結束後淡出秒數")]
        [SerializeField] private float videoFadeOut = 0.8f;

        // ── 執行時參照 ────────────────────────────────────────────────────────
        private Image    _fadeOverlay;
        private RawImage _videoDisplay;
        private bool     _skipVideo;

        // ── 生命週期 ──────────────────────────────────────────────────────────
        private void Awake()
        {
            EnsureEventSystem();
            BuildUI();
        }

        private IEnumerator Start()
        {
            if (openingVideo != null)
                yield return PlayVideo();

            LoadNext();
        }

        private void Update()
        {
            bool any = Input.GetMouseButtonDown(0)
                    || Input.GetKeyDown(KeyCode.Space)
                    || Input.GetKeyDown(KeyCode.Return)
                    || Input.GetKeyDown(KeyCode.Escape);
            if (any) _skipVideo = true;
        }

        // ── 影片播放 ──────────────────────────────────────────────────────────
        private IEnumerator PlayVideo()
        {
            // 建立 RenderTexture 並掛到顯示層
            var rt = new RenderTexture(1920, 1080, 0);
            _videoDisplay.texture = rt;
            _videoDisplay.gameObject.SetActive(true);

            // 設定 VideoPlayer
            var vp = gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake     = false;
            vp.clip            = openingVideo;
            vp.renderMode      = VideoRenderMode.RenderTexture;
            vp.targetTexture   = rt;
            vp.audioOutputMode = VideoAudioOutputMode.Direct;
            vp.Prepare();

            yield return new WaitUntil(() => vp.isPrepared);

            // 開始播放，遮罩淡出 → 影片出現
            vp.Play();
            _skipVideo = false;
            yield return FadeOverlay(1f, 0f, videoFadeIn);

            // 等待播完或玩家跳過
            yield return new WaitUntil(() => !vp.isPlaying || _skipVideo);

            // 遮罩淡入 → 影片消失
            yield return FadeOverlay(0f, 1f, videoFadeOut);
            vp.Stop();
            Destroy(vp);
            Destroy(rt);
            _videoDisplay.gameObject.SetActive(false);
        }

        // ── 切換場景 ──────────────────────────────────────────────────────────
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
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 影片顯示層（預設隱藏）
            var videoGo = new GameObject("VideoDisplay", typeof(RawImage));
            videoGo.transform.SetParent(canvasGo.transform, false);
            _videoDisplay = videoGo.GetComponent<RawImage>();
            _videoDisplay.raycastTarget = false;
            Stretch(_videoDisplay.rectTransform);
            videoGo.SetActive(false);

            // 淡入 / 淡出遮罩（最後渲染，初始全黑）
            var fadeGo = new GameObject("FadeOverlay", typeof(Image));
            fadeGo.transform.SetParent(canvasGo.transform, false);
            _fadeOverlay = fadeGo.GetComponent<Image>();
            _fadeOverlay.color = Color.black;
            _fadeOverlay.raycastTarget = false;
            Stretch(_fadeOverlay.rectTransform);
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
    }
}
