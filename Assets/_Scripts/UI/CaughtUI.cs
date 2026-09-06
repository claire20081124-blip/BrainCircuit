using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using RunLight.Core;
using RunLight.Player;
using RunLight.Enemy;

namespace RunLight.UI
{
    public class CaughtUI : MonoBehaviour
    {
        public static CaughtUI Instance { get; private set; }

        [Header("時間")]
        [SerializeField] private float animationDelay = 2f;  // 動畫佔位時間，換上真實動畫後調整
        [SerializeField] private float fadeToBlack    = 1.5f;
        [SerializeField] private float textFadeIn     = 0.8f;
        [SerializeField] private float resumeFade     = 0.8f;

        [Header("起始位置（選B時傳送）")]
        [SerializeField] public Transform startPoint;

        private CanvasGroup           _overlay;
        private CanvasGroup           _choiceGroup;
        private bool                  _active;
        private FirstPersonController _fpc;
        private AISweeperController   _catcher;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildUI();
        }

        public static void TriggerCaught(AISweeperController catcher = null)
        {
            if (Instance != null && !Instance._active)
            {
                Instance._catcher = catcher;
                Instance.StartCoroutine(Instance.CaughtSequence());
            }
        }

        private IEnumerator CaughtSequence()
        {
            _active = true;

            // 停止玩家移動（存起來供後續使用）
            _fpc = FindObjectOfType<FirstPersonController>();
            if (_fpc != null) _fpc.MovementLocked = true;

            // 動畫播放佔位（之後換成 Animator.Play + WaitUntil）
            yield return new WaitForSeconds(animationDelay);

            // 慢慢變黑
            yield return Fade(_overlay, 0f, 1f, fadeToBlack);

            // 解鎖游標讓玩家點選項
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            // 顯示選項
            _choiceGroup.gameObject.SetActive(true);
            _choiceGroup.blocksRaycasts = true;
            yield return Fade(_choiceGroup, 0f, 1f, textFadeIn);
        }

        private void ChoiceA()  // 智力歸0，留在原地；清道夫重置
        {
            var stats = PlayerStats.Instance;
            if (stats != null) stats.TakeDamage(stats.MaxBrainPower);
            _catcher?.ResetToStart();
            StartCoroutine(Resume(false));
        }

        private void ChoiceB()  // 智力保留，回到起始房間
        {
            if (_fpc != null && startPoint != null)
            {
                var cc = _fpc.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                _fpc.transform.position = startPoint.position;
                if (cc != null) cc.enabled = true;
            }
            else if (_fpc != null && startPoint == null)
            {
                UnityEngine.Debug.LogWarning("[CaughtUI] Start Point 未設定，無法傳送");
            }
            StartCoroutine(Resume(true));
        }

        private IEnumerator Resume(bool teleported)
        {
            _choiceGroup.blocksRaycasts = false;
            yield return Fade(_choiceGroup, 1f, 0f, 0.3f);
            _choiceGroup.gameObject.SetActive(false);

            yield return Fade(_overlay, 1f, 0f, resumeFade);

            if (_fpc != null) _fpc.MovementLocked = false;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            _active = false;
        }

        private IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            float t = 0f;
            group.alpha = from;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }

        private void BuildUI()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("CaughtCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            // 全黑遮罩
            var overlayGo = new GameObject("Overlay", typeof(Image));
            overlayGo.transform.SetParent(canvasGo.transform, false);
            var overlayImg = overlayGo.GetComponent<Image>();
            overlayImg.color = Color.black;
            var oRt = overlayGo.GetComponent<RectTransform>();
            oRt.anchorMin = Vector2.zero; oRt.anchorMax = Vector2.one;
            oRt.offsetMin = oRt.offsetMax = Vector2.zero;
            _overlay = overlayGo.AddComponent<CanvasGroup>();
            _overlay.alpha = 0f;
            _overlay.blocksRaycasts = false;

            // 選項面板
            var choiceGo = new GameObject("ChoicePanel");
            choiceGo.transform.SetParent(canvasGo.transform, false);
            var cRt = choiceGo.AddComponent<RectTransform>();
            cRt.anchorMin = Vector2.zero; cRt.anchorMax = Vector2.one;
            cRt.offsetMin = cRt.offsetMax = Vector2.zero;
            _choiceGroup = choiceGo.AddComponent<CanvasGroup>();
            _choiceGroup.alpha = 0f;
            _choiceGroup.blocksRaycasts = false;
            choiceGo.SetActive(false);

            // 標題
            MakeLabel(choiceGo.transform, font, "你被逮捕了",
                60, Color.white, new Vector2(0.5f, 0.62f), new Vector2(800f, 80f));

            // 副標
            MakeLabel(choiceGo.transform, font, "你的意識將如何延續？",
                28, new Color(1f, 1f, 1f, 0.65f), new Vector2(0.5f, 0.53f), new Vector2(700f, 40f));

            // 按鈕 A
            MakeButton(choiceGo.transform, font,
                "反抗", "智力歸零・留在原地",
                new Vector2(0.5f, 0.38f), new Vector2(-210f, 0f),
                new Color(0.55f, 0.1f, 0.1f, 0.9f),
                ChoiceA);

            // 按鈕 B
            MakeButton(choiceGo.transform, font,
                "順從", "智力保留・回到初始房間",
                new Vector2(0.5f, 0.38f), new Vector2(210f, 0f),
                new Color(0.1f, 0.3f, 0.6f, 0.9f),
                ChoiceB);
        }

        private void MakeLabel(Transform parent, Font font, string text, int size,
            Color color, Vector2 anchor, Vector2 sizeDelta)
        {
            var go = new GameObject("Lbl", typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            go.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.85f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = sizeDelta;
        }

        private void MakeButton(Transform parent, Font font, string title, string subtitle,
            Vector2 anchor, Vector2 offset, Color bgColor,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bgColor;
            go.GetComponent<Button>().onClick.AddListener(onClick);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(360f, 110f);

            // 主標題（大字）
            var titleGo = new GameObject("Title", typeof(Text));
            titleGo.transform.SetParent(go.transform, false);
            var tt = titleGo.GetComponent<Text>();
            tt.font = font; tt.text = title; tt.fontSize = 38;
            tt.fontStyle = FontStyle.Bold;
            tt.color = Color.white; tt.alignment = TextAnchor.MiddleCenter;
            tt.raycastTarget = false;
            tt.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleGo.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            var trt = titleGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.45f); trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            // 副說明（小字）
            var subGo = new GameObject("Sub", typeof(Text));
            subGo.transform.SetParent(go.transform, false);
            var st = subGo.GetComponent<Text>();
            st.font = font; st.text = subtitle; st.fontSize = 17;
            st.color = new Color(1f, 1f, 1f, 0.75f); st.alignment = TextAnchor.MiddleCenter;
            st.raycastTarget = false;
            st.horizontalOverflow = HorizontalWrapMode.Overflow;
            var srt = subGo.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = new Vector2(1f, 0.5f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;
        }
    }
}
