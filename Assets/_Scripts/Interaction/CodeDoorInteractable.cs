using UnityEngine;
using UnityEngine.UI;
using RunLight.Core;

namespace RunLight.Interaction
{
    public class CodeDoorInteractable : MonoBehaviour
    {
        public static bool IsInputting  { get; private set; }
        public static bool HasCandidate => _candidate != null && !_candidate._isOpen;

        [Header("開門動畫")]
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float openSpeed = 4f;

        [Header("密碼（留空則不需要密碼）")]
        [SerializeField] private string doorId      = "codedoor_01"; // 每扇門要唯一
        [SerializeField] private string correctCode = "0613";

        [Header("智力門檻（0 = 不限制）")]
        [SerializeField] private int minBrainPower = 0;

        [Header("開門方向")]
        [Tooltip("勾選後左右方向互換")]
        [SerializeField] private bool flipSide = false;

        [Header("互動距離")]
        [SerializeField] private float interactRange = 3f;

        [Header("音效")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;

        [Header("門口擋牆（開門時關閉）")]
        [SerializeField] private Collider doorBlocker;

        // 每幀只有最適合的那扇門顯示提示
        private static CodeDoorInteractable _candidate;
        private static float _candidateScore; // 越小越好（角度）
        private static int   _candidateFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _candidate = null; _candidateFrame = -1; }

        private float       _showInsufficientTimer;
        private Quaternion  _closedRot;
        private Quaternion  _openRot;
        private bool        _isOpen;
        private bool        _unlocked;
        private bool        _saved;
        private Transform   _player;
        private AudioSource _audio;
        private RunLight.Player.FirstPersonController _fpc;
        private Camera      _cam;

        // UI
        private GameObject _ui;
        private Text       _inputDisplay;
        private Text       _feedbackTxt;
        private string     _inputBuffer = "";
        private Font       _font;

        private void Start()
        {
            _closedRot = transform.localRotation;
            _openRot   = _closedRot * Quaternion.Euler(0f, openAngle, 0f);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var go = GameObject.FindWithTag("Player");
            if (go != null) { _player = go.transform; _fpc = go.GetComponent<RunLight.Player.FirstPersonController>(); }
            if (_player == null)
            {
                _fpc = FindObjectOfType<RunLight.Player.FirstPersonController>();
                if (_fpc != null) _player = _fpc.transform;
            }

            _cam = _fpc != null ? _fpc.GetComponentInChildren<Camera>() : null;
            if (_cam == null) _cam = Camera.main;

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 0f;
            _audio.playOnAwake  = false;
            _audio.volume       = soundVolume;

            // 讀取存檔中的解鎖狀態
            if (Core.GameManager.HasInstance &&
                Core.GameManager.Instance.Flags.GetBool(doorId, false))
            {
                _unlocked = true;
                _isOpen   = true;
                if (doorBlocker != null) doorBlocker.enabled = false;
                transform.localRotation = _openRot;
            }

            BuildUI();
        }

        private void Update()
        {
            if (_player == null) return;
            float dist = Vector3.Distance(transform.position, _player.position);

            // 每幀投票：用攝影機看向角度選最合適的門
            if (!IsInputting && dist <= interactRange)
            {
                if (_candidateFrame != Time.frameCount)
                {
                    _candidateFrame = Time.frameCount;
                    _candidate      = null;
                    _candidateScore = float.MaxValue;
                }
                if (_cam != null)
                {
                    var toDir  = (transform.position - _cam.transform.position).normalized;
                    float angle = Vector3.Angle(_cam.transform.forward, toDir);
                    if (angle < 70f && angle < _candidateScore)
                    {
                        _candidateScore = angle;
                        _candidate      = this;
                    }
                }
            }

            if (IsInputting)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) CloseUI();

                for (int d = 0; d <= 9; d++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha0 + d) ||
                        Input.GetKeyDown(KeyCode.Keypad0 + d))
                        AppendDigit(d.ToString());
                }
                if (Input.GetKeyDown(KeyCode.Backspace))                          DeleteLast();
                if (Input.GetKeyDown(KeyCode.Return) ||
                    Input.GetKeyDown(KeyCode.KeypadEnter))                        SubmitCode();
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.E) && dist <= interactRange
                    && !NoteReadable.IsReading && !UI.BackpackUI.IsOpen
                    && _candidate == this)
                {
                    // 智力門檻檢查
                    if (minBrainPower > 0 && !_unlocked)
                    {
                        int bp = Core.PlayerStats.Instance != null
                            ? Core.PlayerStats.Instance.CurrentBrainPower : 0;
                        if (bp < minBrainPower) { _showInsufficientTimer = 2f; return; }
                        // 智力足夠，直接解鎖不需密碼
                        _unlocked = true;
                        if (Core.GameManager.HasInstance)
                            Core.GameManager.Instance.Flags.SetBool(doorId, true);
                    }

                    if (_unlocked)
                    {
                        if (_isOpen) CloseDoor();
                        else         OpenDoor();
                    }
                    else
                    {
                        OpenUI();
                    }
                }
            }

            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                _isOpen ? _openRot : _closedRot,
                Time.deltaTime * openSpeed);

            if (_showInsufficientTimer > 0f) _showInsufficientTimer -= Time.deltaTime;
        }

        // ── 數字輸入 ──────────────────────────────────────────
        private void AppendDigit(string d)
        {
            if (_inputBuffer.Length >= correctCode.Length) return;
            _inputBuffer += d;
            if (_feedbackTxt != null) _feedbackTxt.text = "";
            UpdateDisplay();
        }

        private void DeleteLast()
        {
            if (_inputBuffer.Length > 0)
                _inputBuffer = _inputBuffer[..^1];
            UpdateDisplay();
        }

        private void SubmitCode()
        {
            if (_inputBuffer == correctCode)
            {
                _unlocked = true;
                if (Core.GameManager.HasInstance)
                    Core.GameManager.Instance.Flags.SetBool(doorId, true);
                CloseUI();
            }
            else
            {
                _inputBuffer = "";
                if (_feedbackTxt != null)
                {
                    _feedbackTxt.text  = "密碼錯誤";
                    _feedbackTxt.color = new Color(0.9f, 0.2f, 0.2f);
                }
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (_inputDisplay == null) return;
            _inputDisplay.text = _inputBuffer.Length > 0 ? _inputBuffer : "────";
        }

        // ── 門控制 ────────────────────────────────────────────
        private void OpenDoor()
        {
            // 用關門狀態的方向判斷側邊，避免動畫中途按 E 導致方向算錯
            Vector3 toPlayer    = (_player.position - transform.position).normalized;
            Vector3 closedFwd   = _closedRot * Vector3.forward;
            Vector3 closedRight = _closedRot * Vector3.right;
            float sideF = Vector3.Dot(closedFwd,   toPlayer);
            float sideR = Vector3.Dot(closedRight,  toPlayer);
            float side  = Mathf.Abs(sideF) >= Mathf.Abs(sideR) ? sideF : sideR;
            if (flipSide) side = -side;
            float angle = side >= 0f ? -openAngle : openAngle;
            _openRot = _closedRot * Quaternion.Euler(0f, angle, 0f);

            _isOpen = true;
            if (doorBlocker != null) doorBlocker.enabled = false;
            if (openSound != null) { _audio.clip = openSound; _audio.Play(); }
            if (!_saved)
            {
                _saved = true;
                if (GameManager.HasInstance) GameManager.Instance.SaveGame();
            }
        }

        private void CloseDoor()
        {
            _isOpen = false;
            if (doorBlocker != null) doorBlocker.enabled = true;
            if (closeSound != null) { _audio.clip = closeSound; _audio.Play(); }
        }

        // ── UI 開關 ───────────────────────────────────────────
        private void OpenUI()
        {
            IsInputting  = true;
            _inputBuffer = "";
            if (_feedbackTxt != null) _feedbackTxt.text = "";
            UpdateDisplay();
            _ui?.SetActive(true);
            if (_fpc != null) _fpc.MovementLocked = true;
        }

        private void CloseUI()
        {
            IsInputting = false;
            _ui?.SetActive(false);
            if (_fpc != null) _fpc.MovementLocked = false;
        }

        // ── 建 UI ─────────────────────────────────────────────
        private void BuildUI()
        {
            var canvasGo = new GameObject("CodeDoorCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            // 遮罩
            var overlay = new GameObject("Overlay", typeof(Image));
            overlay.transform.SetParent(canvasGo.transform, false);
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
            var ort = overlay.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = ort.offsetMax = Vector2.zero;

            // 面板
            var panel = new GameObject("Panel", typeof(Image));
            panel.transform.SetParent(overlay.transform, false);
            panel.GetComponent<Image>().color = new Color(0.07f, 0.09f, 0.13f, 0.97f);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.36f, 0.22f); prt.anchorMax = new Vector2(0.64f, 0.78f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;

            // 標題
            var title = MakeTxt("Title", panel.transform, "輸入密碼", 26,
                new Color(0.55f, 0.80f, 1f), FontStyle.Bold);
            SetAnchors(title.rectTransform, 0f, 0.88f, 1f, 1f, 0f, -14f, 0f, -10f);
            title.alignment = TextAnchor.MiddleCenter;

            // 輸入顯示框
            var inputBg = new GameObject("InputBg", typeof(Image));
            inputBg.transform.SetParent(panel.transform, false);
            inputBg.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.09f, 1f);
            SetAnchors(inputBg.GetComponent<RectTransform>(), 0.08f, 0.73f, 0.92f, 0.87f);

            _inputDisplay = MakeTxt("Display", inputBg.transform, "────", 30, Color.white, FontStyle.Bold);
            SetAnchors(_inputDisplay.rectTransform, 0f, 0f, 1f, 1f);
            _inputDisplay.alignment = TextAnchor.MiddleCenter;

            // 錯誤提示
            _feedbackTxt = MakeTxt("Feedback", panel.transform, "", 18,
                new Color(0.9f, 0.2f, 0.2f), FontStyle.Normal);
            SetAnchors(_feedbackTxt.rectTransform, 0f, 0.67f, 1f, 0.73f);
            _feedbackTxt.alignment = TextAnchor.MiddleCenter;

            // 數字鍵盤：1 2 3 / 4 5 6 / 7 8 9 / ← 0 ✓
            string[] labels = { "1","2","3","4","5","6","7","8","9","←","0","✓" };
            for (int i = 0; i < labels.Length; i++)
            {
                int col = i % 3;
                int row = i / 3;
                float xMin = 0.06f + col * 0.31f;
                float xMax = xMin + 0.27f;
                float yMax = 0.63f - row * 0.155f;
                float yMin = yMax - 0.13f;

                var lbl = labels[i];
                var btn = MakeBtn(lbl, panel.transform,
                    new Vector2(xMin, yMin), new Vector2(xMax, yMax),
                    new Color(0.14f, 0.17f, 0.24f, 0.95f));
                btn.onClick.AddListener(() =>
                {
                    if      (lbl == "←") DeleteLast();
                    else if (lbl == "✓") SubmitCode();
                    else                  AppendDigit(lbl);
                });
            }

            // 關閉按鈕（右上角）
            var closeBtn = MakeBtn("✕", panel.transform,
                new Vector2(0.78f, 0.90f), new Vector2(0.96f, 0.99f),
                new Color(0.35f, 0.10f, 0.10f, 0.90f));
            closeBtn.onClick.AddListener(CloseUI);

            _ui = canvasGo;
            _ui.SetActive(false);
        }

        private Text MakeTxt(string name, Transform parent, string content,
            int size, Color color, FontStyle style)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = _font; t.text = content;
            t.fontSize = size; t.color = color; t.fontStyle = style;
            t.raycastTarget = false;
            return t;
        }

        private Button MakeBtn(string label, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            var go = new GameObject("Btn_" + label, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bgColor;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(3f, 3f); rt.offsetMax = new Vector2(-3f, -3f);

            var txt = MakeTxt(label + "L", go.transform, label, 26, Color.white, FontStyle.Bold);
            SetAnchors(txt.rectTransform, 0f, 0f, 1f, 1f);
            txt.alignment     = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            return go.GetComponent<Button>();
        }

        private static void SetAnchors(RectTransform rt,
            float xMin, float yMin, float xMax, float yMax,
            float offL = 0, float offB = 0, float offR = 0, float offT = 0)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = new Vector2(offL, offB);
            rt.offsetMax = new Vector2(offR, offT);
        }

        private void LateUpdate()
        {
            if (_player == null || IsInputting || _candidate != this) return;
            if (_showInsufficientTimer > 0f)
                InteractionHint.SubmitWarning($"智力不足（需要 {minBrainPower}）");
            string msg = _unlocked ? "按 E 開/關門"
                : minBrainPower > 0 ? $"按 E 開門（需要智力 {minBrainPower}）"
                : "按 E 輸入密碼";
            InteractionHint.Submit(msg, 10);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
