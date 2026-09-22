using UnityEngine;
using UnityEngine.UI;
using RunLight.Player;

namespace RunLight.Interaction
{
    public class NoteReadable : MonoBehaviour
    {
        public static bool IsReading { get; private set; }

        [TextArea(3, 8)]
        [SerializeField] private string noteContent   = "密碼：0613";
        [SerializeField] private float  interactRange = 2.5f;

        private Transform             _player;
        private FirstPersonController _fpc;
        private GameObject            _ui;
        private Font                  _font;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var go = GameObject.FindWithTag("Player");
            if (go != null) { _player = go.transform; _fpc = go.GetComponent<FirstPersonController>(); }
            if (_player == null)
            {
                _fpc = FindObjectOfType<FirstPersonController>();
                if (_fpc != null) _player = _fpc.transform;
            }

            BuildUI();
        }

        private void Update()
        {
            if (_player == null) return;
            float dist = Vector3.Distance(transform.position, _player.position);

            if (!IsReading && dist <= interactRange && Input.GetKeyDown(KeyCode.E)
                && !CodeDoorInteractable.IsInputting
                && !UI.BackpackUI.IsOpen)
            {
                Show();
            }
            else if (IsReading && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)))
            {
                Hide();
            }
        }

        private void Show()
        {
            IsReading = true;
            _ui?.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            if (_fpc != null) _fpc.MovementLocked = true;
        }

        private void Hide()
        {
            IsReading = false;
            _ui?.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            if (_fpc != null) _fpc.MovementLocked = false;
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("NoteCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            // 半透明遮罩
            var overlay = new GameObject("Overlay", typeof(Image));
            overlay.transform.SetParent(canvasGo.transform, false);
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.80f);
            var ort = overlay.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = ort.offsetMax = Vector2.zero;

            // 紙張
            var paper = new GameObject("Paper", typeof(Image));
            paper.transform.SetParent(overlay.transform, false);
            paper.GetComponent<Image>().color = new Color(0.93f, 0.89f, 0.78f, 1f);
            var prt = paper.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.30f, 0.22f); prt.anchorMax = new Vector2(0.70f, 0.78f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;

            // 紙條文字
            var textGo = new GameObject("NoteText", typeof(Text));
            textGo.transform.SetParent(paper.transform, false);
            var txt = textGo.GetComponent<Text>();
            txt.font      = _font;
            txt.text      = noteContent;
            txt.fontSize  = 28;
            txt.color     = new Color(0.10f, 0.08f, 0.05f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.05f, 0.15f); trt.anchorMax = new Vector2(0.95f, 0.88f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            // 關閉提示
            var hintGo = new GameObject("Hint", typeof(Text));
            hintGo.transform.SetParent(paper.transform, false);
            var hint = hintGo.GetComponent<Text>();
            hint.font      = _font;
            hint.text      = "按 E 或 ESC 關閉";
            hint.fontSize  = 18;
            hint.color     = new Color(0.40f, 0.35f, 0.28f);
            hint.alignment = TextAnchor.MiddleCenter;
            hint.raycastTarget = false;
            var hrt = hintGo.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 0.02f); hrt.anchorMax = new Vector2(1f, 0.14f);
            hrt.offsetMin = hrt.offsetMax = Vector2.zero;

            _ui = canvasGo;
            _ui.SetActive(false);
        }

        private void OnGUI()
        {
            if (IsReading || _player == null) return;
            if (Vector3.Distance(transform.position, _player.position) > interactRange) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 20, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };
            GUI.Label(new Rect((Screen.width - 300f) * 0.5f, Screen.height * 0.68f, 300f, 32f),
                "按 E 查看紙條", style);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
