using UnityEngine;

namespace RunLight.Enemy
{
    public class SecurityCameraController : MonoBehaviour
    {
        [Header("旋轉巡邏")]
        [SerializeField] private float sweepAngle  = 60f;   // 左右各幾度
        [SerializeField] private float sweepSpeed  = 25f;   // 度/秒

        [Header("偵測")]
        [SerializeField] private float detectRange = 10f;
        [SerializeField] private float detectAngle = 30f;   // 視野半角
        [SerializeField] private LayerMask obstacleMask = ~0;

        [Header("警報")]
        [Tooltip("留空則警報所有清道夫，有填則只警報最近這幾個")]
        [SerializeField] private AISweeperController[] linkedSweepers;

        [Header("指示燈（選填，有 Light 組件）")]
        [SerializeField] private Light indicatorLight;
        [SerializeField] private Color normalColor  = Color.green;
        [SerializeField] private Color alertColor   = Color.red;

        private Transform _player;
        private float     _sweepT;
        private bool      _sweepForward = true;
        private bool      _alerted;

        private void Start()
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) _player = go.transform;
            if (indicatorLight != null) indicatorLight.color = normalColor;
        }

        private void Update()
        {
            Sweep();

            if (_player != null && !AISweeperController.PlayerIsHidingStatic() && DetectsPlayer())
                TriggerAlert();
            else if (_alerted)
                ClearAlert();
        }

        private void Sweep()
        {
            float half = sweepAngle * 0.5f;
            _sweepT += (_sweepForward ? 1f : -1f) * sweepSpeed * Time.deltaTime;
            if (_sweepT >= half)  { _sweepT = half;  _sweepForward = false; }
            if (_sweepT <= -half) { _sweepT = -half; _sweepForward = true;  }
            transform.localRotation = Quaternion.Euler(0f, _sweepT, 0f);
        }

        private bool DetectsPlayer()
        {
            var dir = _player.position - transform.position;
            if (dir.magnitude > detectRange) return false;
            if (Vector3.Angle(transform.forward, dir) > detectAngle) return false;

            // 視線遮擋檢查
            if (Physics.Raycast(transform.position, dir.normalized, out var hit, detectRange, obstacleMask))
                if (!hit.transform.IsChildOf(_player) && hit.transform != _player)
                    return false;

            return true;
        }

        private void TriggerAlert()
        {
            if (_alerted) return;
            _alerted = true;
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

        private void ClearAlert()
        {
            _alerted = false;
            if (indicatorLight != null) indicatorLight.color = normalColor;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectRange);
            var left  = Quaternion.Euler(0, -detectAngle, 0) * transform.forward;
            var right = Quaternion.Euler(0,  detectAngle, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, left  * detectRange);
            Gizmos.DrawRay(transform.position, right * detectRange);
        }
    }
}
