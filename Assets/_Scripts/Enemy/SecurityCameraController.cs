using UnityEngine;

namespace RunLight.Enemy
{
    public class SecurityCameraController : MonoBehaviour
    {
        [Header("旋轉巡邏")]
        [SerializeField] private float sweepAngle = 60f;   // 左右各幾度
        [SerializeField] private float sweepSpeed = 25f;   // 度/秒

        [Header("偵測")]
        [SerializeField] private float detectRange = 10f;
        [SerializeField] private float detectAngle = 30f;   // 視野半角
        [SerializeField] private float detectTime  = 1.5f;  // 盯幾秒才觸發
        [SerializeField] private float decayRate   = 0.5f;  // 離開視野後每秒減少多少（0～1）
        [SerializeField] private LayerMask obstacleMask = ~0;

        [Header("警報")]
        [Tooltip("留空則直接觸發被抓；有填則警報這幾個清道夫")]
        [SerializeField] private AISweeperController[] linkedSweepers;

        [Header("指示燈（選填，有 Light 組件）")]
        [SerializeField] private Light indicatorLight;
        [SerializeField] private Color normalColor  = Color.green;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color alertColor   = Color.red;

        private Transform  _player;
        private float      _sweepT;
        private bool       _sweepForward = true;
        private float      _suspicion;   // 0 ~ 1
        private bool       _triggered;
        private Quaternion _baseRot;

        private void Start()
        {
            _baseRot = transform.localRotation;
            var go = GameObject.FindWithTag("Player");
            if (go != null) _player = go.transform;
            else Debug.LogWarning("[SecurityCamera] 找不到 Player 標籤的物件！");
            if (indicatorLight != null) indicatorLight.color = normalColor;
        }

        private void Update()
        {
            if (_triggered) return;

            Sweep();
            Debug.DrawRay(transform.position, transform.forward * detectRange, Color.red);

            bool inSight = _player != null
                && !AISweeperController.PlayerIsHidingStatic()
                && DetectsPlayer();

            if (inSight)
            {
                _suspicion += Time.deltaTime / detectTime;
                if (_suspicion >= 1f)
                {
                    _suspicion = 1f;
                    Trigger();
                }
            }
            else
            {
                _suspicion = Mathf.Max(0f, _suspicion - Time.deltaTime * decayRate);
            }

            UpdateLight();
        }

        private void Sweep()
        {
            float half = sweepAngle * 0.5f;
            _sweepT += (_sweepForward ? 1f : -1f) * sweepSpeed * Time.deltaTime;
            if (_sweepT >= half)  { _sweepT = half;  _sweepForward = false; }
            if (_sweepT <= -half) { _sweepT = -half; _sweepForward = true;  }
            transform.localRotation = _baseRot * Quaternion.Euler(0f, _sweepT, 0f);
        }

        private float _debugTimer;
        private bool DetectsPlayer()
        {
            var dir = _player.position - transform.position;
            float dist  = dir.magnitude;
            float angle = Vector3.Angle(transform.forward, dir);

            _debugTimer -= Time.deltaTime;
            if (_debugTimer <= 0f)
            {
                _debugTimer = 1f;
                Debug.Log($"[Camera] dist={dist:F1}/{detectRange}  angle={angle:F1}/{detectAngle}  hiding={AISweeperController.PlayerIsHidingStatic()}");
            }

            if (dist > detectRange) return false;
            if (angle > detectAngle) return false;
            if (AISweeperController.PlayerIsHidingStatic()) return false;

            var origin   = transform.position + transform.forward * 0.3f;
            var toPlayer = _player.position - origin;
            if (Physics.Raycast(origin, toPlayer.normalized, out var hit, toPlayer.magnitude, obstacleMask))
            {
                if (!hit.transform.IsChildOf(_player) && hit.transform != _player)
                {
                    Debug.Log($"[Camera] 射線被 {hit.transform.name} 擋住");
                    return false;
                }
            }
            return true;
        }

        private void Trigger()
        {
            _triggered = true;
            if (indicatorLight != null) indicatorLight.color = alertColor;

            if (linkedSweepers != null && linkedSweepers.Length > 0)
            {
                foreach (var s in linkedSweepers)
                    if (s != null) s.AlertChase();
            }
            else
            {
                foreach (var s in FindObjectsByType<AISweeperController>(FindObjectsSortMode.None))
                    s.AlertChase();
            }
        }

        private void UpdateLight()
        {
            if (indicatorLight == null) return;
            indicatorLight.color = _suspicion > 0f
                ? Color.Lerp(warningColor, alertColor, _suspicion)
                : normalColor;
        }

        // 外部呼叫重置（例如從存檔點恢復）
        public void ResetCamera()
        {
            _triggered = false;
            _suspicion  = 0f;
            if (indicatorLight != null) indicatorLight.color = normalColor;
        }

        private void OnDrawGizmosSelected()
        {
            // 紅線 = Forward 方向（鏡頭朝這裡看）
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * detectRange);

            // 青線 = 偵測視野邊緣
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectRange);
            var left  = Quaternion.Euler(0, -detectAngle, 0) * transform.forward;
            var right = Quaternion.Euler(0,  detectAngle, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, left  * detectRange);
            Gizmos.DrawRay(transform.position, right * detectRange);
        }
    }
}
