using UnityEngine;
using UnityEngine.UI;
using RunLight.Player;

namespace RunLight.UI
{
    public class StaminaBarUI : MonoBehaviour
    {
        [SerializeField] private float fadeSpeed  = 4f;
        [SerializeField] private float barWidth   = 360f;
        [SerializeField] private float barHeight  = 20f;
        [SerializeField] private float barOffsetY = 60f;

        private FirstPersonController _player;
        private CanvasGroup           _group;
        private Image                 _fill;
        private RectTransform         _fillRect;

        private static readonly Color ColFull  = new Color(0.25f, 0.75f, 1f);
        private static readonly Color ColEmpty = new Color(0.9f, 0.15f, 0.1f);

        private void Start()
        {
            _player = FindObjectOfType<FirstPersonController>();
            BuildUI();
        }

        private void BuildUI()
        {
            var root = new GameObject("StaminaCanvas");
            DontDestroyOnLoad(root);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            _group = root.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            // 背景條
            var bg     = new GameObject("BG");
            bg.transform.SetParent(root.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin        = new Vector2(0.5f, 0f);
            bgRect.anchorMax        = new Vector2(0.5f, 0f);
            bgRect.pivot            = new Vector2(0.5f, 0f);
            bgRect.anchoredPosition = new Vector2(0f, barOffsetY);
            bgRect.sizeDelta        = new Vector2(barWidth, barHeight);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);

            // 填充條（anchor 控制兩側收縮）
            var fill = new GameObject("Fill");
            fill.transform.SetParent(bg.transform, false);
            _fillRect = fill.AddComponent<RectTransform>();
            _fillRect.anchorMin = Vector2.zero;
            _fillRect.anchorMax = Vector2.one;
            _fillRect.sizeDelta = Vector2.zero;
            _fill = fill.AddComponent<Image>();
            _fill.color = ColFull;
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindObjectOfType<FirstPersonController>();
                return;
            }

            float ratio = _player.StaminaRatio;

            bool shouldShow = _player.IsSprinting || ratio < 1f;
            _group.alpha = Mathf.MoveTowards(_group.alpha, shouldShow ? 1f : 0f, fadeSpeed * Time.deltaTime);

            // 兩側同時向中間縮短
            float edge = (1f - ratio) * 0.5f;
            _fillRect.anchorMin = new Vector2(edge, 0f);
            _fillRect.anchorMax = new Vector2(1f - edge, 1f);

            // 藍（滿）→ 紅（空）
            _fill.color = Color.Lerp(ColEmpty, ColFull, ratio);
        }
    }
}
