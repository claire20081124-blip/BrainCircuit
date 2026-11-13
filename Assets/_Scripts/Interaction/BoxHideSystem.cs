using System.Collections;
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

        [Tooltip("玩家手部模型（躲藏時隱藏）")]
        [SerializeField] private GameObject handModel;

        [Header("音效")]
        [SerializeField] private AudioClip enterSound;
        [SerializeField] private AudioClip exitSound;
        [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;

        [Tooltip("躲藏時相機下移量（正值往下）")]
        [SerializeField] private float crouchCameraOffset = 1.2f;
        [SerializeField] private float crouchSpeed = 4f;

        private bool _hasBox;
        private bool _isHiding;
        private GameObject _overlay;
        private FirstPersonController _fpc;
        private Transform _camTransform;
        private float _camStandY;
        private Coroutine _crouchRoutine;
        private AudioSource _audio;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (giveBoxOnStart) _hasBox = true;

            var go = GameObject.FindWithTag("Player");
            if (go != null) _fpc = go.GetComponent<FirstPersonController>();
            if (_fpc == null) _fpc = FindObjectOfType<FirstPersonController>();

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 0f;
            _audio.playOnAwake  = false;
            _audio.volume       = soundVolume;

            // 找相機
            var cam = Camera.main;
            if (cam != null) _camTransform = cam.transform;
            if (_camTransform != null) _camStandY = _camTransform.localPosition.y;

            BuildOverlay();
        }

        private void Update()
        {
            if (!_hasBox) return;
            if (BackpackUI.IsOpen) return;
            if (CodeDoorInteractable.IsInputting) return;
            if (UI.CaughtUI.Instance != null && UI.CaughtUI.Instance.IsActive) return;

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
            if (_fpc != null) _fpc.MoveOnlyLocked = true;
            if (handModel != null) handModel.SetActive(false);
            _overlay?.SetActive(true);
            SetCrouchTarget(_camStandY - crouchCameraOffset);
            if (enterSound != null) _audio.PlayOneShot(enterSound, soundVolume);
        }

        private void ExitBox()
        {
            _isHiding = false;
            if (_fpc != null) _fpc.MoveOnlyLocked = false;
            if (handModel != null) handModel.SetActive(true);
            _overlay?.SetActive(false);
            SetCrouchTarget(_camStandY);
            if (exitSound != null) _audio.PlayOneShot(exitSound, soundVolume);
        }

        private void SetCrouchTarget(float targetY)
        {
            if (_camTransform == null) return;
            if (_crouchRoutine != null) StopCoroutine(_crouchRoutine);
            _crouchRoutine = StartCoroutine(SmoothCamY(targetY));
        }

        private IEnumerator SmoothCamY(float targetY)
        {
            while (Mathf.Abs(_camTransform.localPosition.y - targetY) > 0.001f)
            {
                var p = _camTransform.localPosition;
                p.y = Mathf.Lerp(p.y, targetY, Time.deltaTime * crouchSpeed);
                _camTransform.localPosition = p;
                yield return null;
            }
            var final = _camTransform.localPosition;
            final.y = targetY;
            _camTransform.localPosition = final;
        }

        // 外部呼叫：讓紙箱進背包（WorldPickup 拾取時呼叫）
        public void PickUpBox() => _hasBox = true;

        // ── 紙箱視角 UI ───────────────────────────────────────────
        private void BuildOverlay()
        {
            var canvasGo = new GameObject("BoxOverlay", typeof(Canvas));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;

            var rawGo = new GameObject("Mask", typeof(UnityEngine.UI.RawImage));
            rawGo.transform.SetParent(canvasGo.transform, false);
            var ri = rawGo.GetComponent<UnityEngine.UI.RawImage>();
            ri.texture = BuildBoxMaskTexture();
            var rt = rawGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            _overlay = canvasGo;
            _overlay.SetActive(false);
        }

        private static Texture2D BuildBoxMaskTexture()
        {
            const int tw = 480, th = 270; // 1/4 解析度，省效能
            var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
            var pixels = new Color32[tw * th];

            // 兩個圓洞中心（1920×1080 座標系）
            const float lx = 620f, rx = 1300f, cy = 540f;
            const float holeR = 230f; // 洞半徑
            const byte  bgA   = 255;  // 完全不透明

            for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                float px = (float)x / tw * 1920f;
                float py = (float)y / th * 1080f;
                float d1 = Mathf.Sqrt((px - lx) * (px - lx) + (py - cy) * (py - cy));
                float d2 = Mathf.Sqrt((px - rx) * (px - rx) + (py - cy) * (py - cy));
                float d  = Mathf.Min(d1, d2);

                byte a = d < holeR ? (byte)0 : bgA;

                pixels[y * tw + x] = new Color32(8, 5, 4, a);
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return tex;
        }

        private void OnGUI()
        {
            if (!_hasBox || _isHiding) return;
            if (UI.CaughtUI.Instance != null && UI.CaughtUI.Instance.IsActive) return;

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
