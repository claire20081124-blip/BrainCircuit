using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RunLight.UI;

namespace RunLight.Interaction
{
    public class LevelExit : MonoBehaviour
    {
        [Header("切換場景")]
        [SerializeField] private string nextScene = "1";

        [Header("白光效果")]
        [SerializeField] private float fadeOutDuration = 1.2f;   // 亮白花幾秒
        [SerializeField] private float holdDuration    = 0.4f;   // 全白維持幾秒

        [Header("音效（選填）")]
        [SerializeField] private AudioClip exitSound;
        [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;

        private bool  _triggered;
        private Image _overlay;

        private void Awake()
        {
            // 建全螢幕白幕
            var canvasGo = new GameObject("ExitCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var ov = new GameObject("WhiteOverlay", typeof(Image));
            ov.transform.SetParent(canvasGo.transform, false);
            _overlay = ov.GetComponent<Image>();
            _overlay.color = new Color(1f, 1f, 1f, 0f);
            _overlay.raycastTarget = false;
            var rt = ov.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (!other.CompareTag("Player")) return;
            _triggered = true;
            if (exitSound != null)
                AudioSource.PlayClipAtPoint(exitSound, transform.position, soundVolume);
            StartCoroutine(ExitSequence());
        }

        private IEnumerator ExitSequence()
        {
            // 淡亮白
            float t = 0f;
            while (t < fadeOutDuration)
            {
                t += Time.deltaTime;
                _overlay.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, t / fadeOutDuration));
                yield return null;
            }
            _overlay.color = Color.white;

            yield return new WaitForSeconds(holdDuration);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            Time.timeScale   = 1f;
            if (!string.IsNullOrEmpty(nextScene))
            {
                if (SceneFader.Instance != null)
                    SceneFader.Instance.FadeOutAndLoad(nextScene);
                else
                    SceneManager.LoadScene(nextScene);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0.3f, 0.4f);
            var col = GetComponent<Collider>();
            if (col is BoxCollider box)
                Gizmos.DrawCube(transform.position + box.center, box.size);
            else
                Gizmos.DrawWireSphere(transform.position, 1.5f);
        }
    }
}
