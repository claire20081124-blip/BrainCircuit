using System.Collections;
using UnityEngine;
using RunLight.Core;

namespace RunLight.Interaction
{
    /// <summary>
    /// 掛在門物件上。玩家靠近按 E 開門，開門時自動存檔一次。
    /// </summary>
    public class DoorInteractable : MonoBehaviour
    {
        [Header("開門動畫")]
        [Tooltip("開門旋轉角度（Y 軸，正值往右開、負值往左開）")]
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float openSpeed = 4f;

        [Header("音效")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;

        [Header("開門方向")]
        [Tooltip("勾選後左右方向互換")]
        [SerializeField] private bool flipSide = false;

        [Header("互動距離")]
        [SerializeField] private float interactRange = 3f;

        [Header("自動關門")]
        [Tooltip("開門後幾秒自動關（0 = 不自動關）")]
        [SerializeField] private float autoCloseDelay = 3f;

        [Header("智力門檻（0 = 不限制）")]
        [SerializeField] private int minBrainPower = 0;

        [Header("門口擋牆（開門時關閉）")]
        [SerializeField] private Collider doorBlocker;

        private static DoorInteractable _candidate;
        private static float _candidateScore;
        private static int   _candidateFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _candidate = null; _candidateFrame = -1; }

        private Quaternion  _closedRot;
        private float       _insufficientTimer;
        private Quaternion  _openRot;
        private bool        _isOpen;
        private bool        _saved;
        private Transform   _player;
        private AudioSource _audio;
        private Camera      _cam;

        private void Start()
        {
            _closedRot = transform.localRotation;

            var go = GameObject.FindWithTag("Player");
            if (go != null) _player = go.transform;

            if (_player == null)
            {
                var fpc = FindObjectOfType<RunLight.Player.FirstPersonController>();
                if (fpc != null) _player = fpc.transform;
            }

            if (_player == null) Debug.LogWarning("[Door] 找不到玩家！");

            var fpcForCam = FindObjectOfType<RunLight.Player.FirstPersonController>();
            _cam = fpcForCam != null ? fpcForCam.GetComponentInChildren<Camera>() : null;
            if (_cam == null) _cam = Camera.main;

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 0f;
            _audio.playOnAwake  = false;
            _audio.volume       = soundVolume;
        }

        private void Update()
        {
            if (_player == null) return;

            float dist = Vector3.Distance(transform.position, _player.position);

            // 每幀投票：看著的那扇門優先
            if (dist <= interactRange)
            {
                if (_candidateFrame != Time.frameCount)
                {
                    _candidateFrame = Time.frameCount;
                    _candidate      = null;
                    _candidateScore = float.MaxValue;
                }
                if (_cam != null)
                {
                    float angle = Vector3.Angle(_cam.transform.forward,
                        (transform.position - _cam.transform.position).normalized);
                    if (angle < _candidateScore) { _candidateScore = angle; _candidate = this; }
                }
            }

            if (Input.GetKeyDown(KeyCode.E) && dist <= interactRange && _candidate == this)
            {
                if (!_isOpen && minBrainPower > 0)
                {
                    int bp = Core.PlayerStats.Instance != null
                        ? Core.PlayerStats.Instance.CurrentBrainPower : 0;
                    if (bp < minBrainPower) { _insufficientTimer = 2f; return; }
                }
                if (_isOpen) Close();
                else Open();
            }

            // 平滑旋轉
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                _isOpen ? _openRot : _closedRot,
                Time.deltaTime * openSpeed);
        }

        private void Open()
        {
            // 用關門狀態的 forward 判斷側邊，避免動畫中途按 E 導致方向算錯
            Vector3 closedForward = _closedRot * Vector3.forward;
            float side = Vector3.Dot(closedForward, (_player.position - transform.position).normalized);
            if (flipSide) side = -side;
            float angle = side >= 0f ? -openAngle : openAngle;
            _openRot = _closedRot * Quaternion.Euler(0f, angle, 0f);

            _isOpen = true;
            if (doorBlocker != null) doorBlocker.enabled = false;
            if (autoCloseDelay > 0f)
            {
                StopAllCoroutines();
                StartCoroutine(AutoCloseRoutine());
            }
            if (openSound != null) { _audio.clip = openSound; _audio.Play(); }

            if (!_saved)
            {
                _saved = true;
                if (GameManager.HasInstance)
                    GameManager.Instance.SaveGame();
            }
        }

        private IEnumerator AutoCloseRoutine()
        {
            yield return new WaitForSeconds(autoCloseDelay);
            if (_isOpen) Close();
        }

        private void Close()
        {
            _isOpen = false;
            if (doorBlocker != null) doorBlocker.enabled = true;
            if (closeSound != null) { _audio.clip = closeSound; _audio.Play(); }
        }

        private void OnGUI()
        {
            if (_player == null || _candidate != this) return;
            if (RunLight.UI.CaughtUI.Instance != null && RunLight.UI.CaughtUI.Instance.IsActive) return;

            if (_insufficientTimer > 0f)
            {
                _insufficientTimer -= Time.deltaTime;
                var warn = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.3f, 0.3f) }
                };
                GUI.Label(new Rect((Screen.width - 400f) * 0.5f, Screen.height * 0.63f, 400f, 32f),
                    $"智力不足（需要 {minBrainPower}）", warn);
            }

            if (!_isOpen && minBrainPower > 0)
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                GUI.Label(new Rect((Screen.width - 400f) * 0.5f, Screen.height * 0.68f, 400f, 32f),
                    $"按 E 開門（需要智力 {minBrainPower}）", style);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
