using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RunLight.UI
{
    /// <summary>
    /// 場景開始時從黑畫面淡入。
    /// 掛在任何 GameObject 上即可，不需要額外設定。
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        [Tooltip("淡入秒數")]
        [SerializeField] private float fadeInDuration = 1.2f;

        private IEnumerator Start()
        {
            var canvas = new GameObject("FadeCanvas", typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            DontDestroyOnLoad(canvas.gameObject);

            var overlay = new GameObject("Overlay", typeof(Image));
            overlay.transform.SetParent(canvas.transform, false);
            var img = overlay.GetComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            var rt = overlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.deltaTime;
                img.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(t / fadeInDuration));
                yield return null;
            }

            Destroy(canvas.gameObject);
        }
    }
}
