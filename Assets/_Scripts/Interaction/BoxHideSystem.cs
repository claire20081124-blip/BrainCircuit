using UnityEngine;
using UnityEngine.UI;
using RunLight.Player;
using RunLight.UI;

namespace RunLight.Interaction
{
    /// <summary>
    /// 紙箱躲藏系統：按 Space 鑽進紙箱，清道夫看不見玩家。
    /// 掛在場景中任一個持久物件（例如 GameManager 或 Player）上。
    /// </summary>
    public class BoxHideSystem : MonoBehaviour
    {
        public static BoxHideSystem Instance { get; private set; }
        public static bool IsHiding => Instance != null && Instance._isHiding;

        [Header("紙箱設定")]
        [Tooltip("開局是否自動給玩家紙箱（不需放置物件時勾）")]
        [SerializeField] private bool giveBoxOnStart = false;

        private bool _hasBox;
        private bool _isHiding;
        private GameObject _overlay;
        private FirstPersonController _fpc;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (giveBoxOnStart) _hasBox = true;

            var go = GameObject.FindWithTag("Player");
            if (go != null) _fpc = go.GetComponent<FirstPersonController>();
            if (_fpc == null) _fpc = FindObjectOfType<FirstPersonController>();

            BuildOverlay();
        }

        private void Update()
        {
            if (!_hasBox) return;
            if (BackpackUI.IsOpen) return;
            if (CodeDoorInteractable.IsInputting) return;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_isHiding) ExitBox();
                else           EnterBox();
            }
        }

        // ── 躲藏控制 ──────────────────────────────────────────────
        private void EnterBox()
        {
            _isHiding = true;
            if (_fpc != null) _fpc.MovementLocked = true;
            _overlay?.SetActive(true);
        }

        private void ExitBox()
        {
            _isHiding = false;
            if (_fpc != null) _fpc.MovementLocked = false;
            _overlay?.SetActive(false);
        }

        // 外部呼叫：讓紙箱進背包（WorldPickup 拾取時呼叫）
        public void PickUpBox() => _hasBox = true;

        // ── 紙箱視角 UI ───────────────────────────────────────────
        private void BuildOverlay()
        {
            var canvasGo = new GameObject("BoxOverlay",
                typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            var black = new Color(0.05f, 0.03f, 0.02f, 1f); // 深棕黑（紙箱內壁）

            // 上方遮罩
            MakePanel(canvasGo.transform, black, 0f, 0.58f, 1f, 1f);
            // 下方遮罩
            MakePanel(canvasGo.transform, black, 0f, 0f,    1f, 0.28f);
            // 左側遮罩
            MakePanel(canvasGo.transform, black, 0f,   0.28f, 0.22f, 0.58f);
            // 中間隔板（兩洞之間）
            MakePanel(canvasGo.transform, black, 0.42f, 0.28f, 0.58f, 0.58f);
            // 右側遮罩
            MakePanel(canvasGo.transform, black, 0.78f, 0.28f, 1f, 0.58f);

            _overlay = canvasGo;
            _overlay.SetActive(false);
        }

        private static void MakePanel(Transform parent, Color color,
            float xMin, float yMin, float xMax, float yMax)
        {
            var go = new GameObject("Panel", typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private void OnGUI()
        {
            if (!_hasBox || _isHiding) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 18,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(1f, 1f, 1f, 0.7f) }
            };
            GUI.Label(new Rect((Screen.width - 300f) * 0.5f, Screen.height * 0.72f, 300f, 28f),
                "按 Space 使用紙箱", style);
        }
    }
}
