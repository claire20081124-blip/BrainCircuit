using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace RunLight.UI
{
    /// <summary>
    /// 跨場景常駐的黑幕淡入/淡出。
    /// 每次場景載入自動淡入；LoadScene() 前呼叫 FadeOutAndLoad() 可先淡黑再切換。
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        private static SceneFader _instance;
        public static SceneFader Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SceneFader");
                    _instance = go.AddComponent<SceneFader>();
                }
                return _instance;
            }
        }

        [SerializeField] private float fadeInDuration  = 0.8f;
        [SerializeField] private float fadeOutDuration = 0.4f;

        private Image _overlay;
        private bool  _busy;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            var canvas = new GameObject("FadeCanvas", typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            canvas.transform.SetParent(transform, false);

            var ov = new GameObject("Overlay", typeof(Image));
            ov.transform.SetParent(canvas.transform, false);
            _overlay = ov.GetComponent<Image>();
            _overlay.color = Color.black;
            _overlay.raycastTarget = false;
            var rt = ov.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene s, LoadSceneMode m)
        {
            StopAllCoroutines();
            _busy = false;
            StartCoroutine(FadeIn());
        }

        // 外部呼叫：淡黑後載入場景
        public void FadeOutAndLoad(string sceneName)
        {
            if (_busy) return;
            _busy = true;
            StartCoroutine(DoFadeOutAndLoad(sceneName));
        }

        private IEnumerator DoFadeOutAndLoad(string sceneName)
        {
            float startAlpha = _overlay.color.a;
            yield return StartCoroutine(Fade(startAlpha, 1f, fadeOutDuration * (1f - startAlpha)));
            try { SceneManager.LoadScene(sceneName); }
            catch (Exception e) { Debug.LogError($"[SceneFader] 無法載入「{sceneName}」：{e.Message}"); _busy = false; }
        }

        private IEnumerator FadeIn()
        {
            _overlay.color = Color.black;
            yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0f;
            _overlay.color = new Color(0f, 0f, 0f, from);
            while (t < duration)
            {
                t += Time.deltaTime;
                _overlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            _overlay.color = new Color(0f, 0f, 0f, to);
        }
    }
}
