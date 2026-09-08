using UnityEngine;
using RunLight.Player;

namespace RunLight.Interaction
{
    public class HidingSpot : MonoBehaviour
    {
        [Header("櫃子門（左門往左開、右門往右開）")]
        [SerializeField] private Transform leftDoor;
        [SerializeField] private Transform rightDoor;
        [SerializeField] private float doorOpenAngle = 110f;
        [SerializeField] private float doorSpeed     = 5f;

        [Header("躲藏位置（在櫃子內放一個空物件）")]
        [SerializeField] private Transform hidePosition;
        [SerializeField] private Transform exitPosition;

        [Header("躲藏朝向（選填：放一個空物件在櫃門前方，進入後視角會轉向它）")]
        [SerializeField] private Transform hideFacing;

        [Header("互動距離")]
        [SerializeField] private float interactRange = 2f;

        public bool IsHiding { get; private set; }

        private Transform             _player;
        private CharacterController   _cc;
        private FirstPersonController _fpc;

        private bool       _doorOpen;
        private Quaternion _leftClosed;
        private Quaternion _rightClosed;
        private bool       _inRange;

        private void Start()
        {
            FindPlayer();
            if (leftDoor  != null) _leftClosed  = leftDoor.localRotation;
            if (rightDoor != null) _rightClosed = rightDoor.localRotation;
        }

        private void FindPlayer()
        {
            var go = GameObject.FindWithTag("Player");
            if (go == null) return;
            _player = go.transform;
            _cc     = go.GetComponent<CharacterController>();
            _fpc    = go.GetComponent<FirstPersonController>();
        }

        private void Update()
        {
            if (_player == null) { FindPlayer(); return; }

            _inRange = Vector3.Distance(transform.position, _player.position) <= interactRange;

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (IsHiding)        ExitHide();
                else if (_inRange)   TryHide();
            }

            if (IsHiding && Input.GetKeyDown(KeyCode.Escape))
                ExitHide();

            AnimateDoors();
        }

        private void TryHide()
        {
            if (hidePosition == null) return;

            IsHiding  = true;
            _doorOpen = true;

            // 傳送進櫃子
            if (_cc != null) _cc.enabled = false;
            _player.position = hidePosition.position;
            if (_cc != null) _cc.enabled = true;

            // 鎖定移動與視角
            if (_fpc != null) _fpc.MovementLocked = true;

            // 轉向指定朝向
            if (hideFacing != null && _fpc != null)
            {
                var dir = hideFacing.position - _player.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    float yaw = Quaternion.LookRotation(dir.normalized).eulerAngles.y;
                    _fpc.ForceRotation(yaw, 0f);
                }
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        public void ForceReveal()
        {
            if (IsHiding) ExitHide();
        }

        private void ExitHide()
        {
            IsHiding  = false;
            _doorOpen = false;

            var dest = exitPosition != null ? exitPosition.position : transform.position;
            if (_cc != null) _cc.enabled = false;
            _player.position = dest;
            if (_cc != null) _cc.enabled = true;

            if (_fpc != null) _fpc.MovementLocked = false;
        }

        private void AnimateDoors()
        {
            if (leftDoor != null)
            {
                var target = _doorOpen
                    ? _leftClosed * Quaternion.Euler(0f, -doorOpenAngle, 0f)
                    : _leftClosed;
                leftDoor.localRotation = Quaternion.Slerp(
                    leftDoor.localRotation, target, Time.deltaTime * doorSpeed);
            }

            if (rightDoor != null)
            {
                var target = _doorOpen
                    ? _rightClosed * Quaternion.Euler(0f, doorOpenAngle, 0f)
                    : _rightClosed;
                rightDoor.localRotation = Quaternion.Slerp(
                    rightDoor.localRotation, target, Time.deltaTime * doorSpeed);
            }
        }

        private void OnGUI()
        {
            if (_fpc == null || _fpc.MovementLocked) return;

            string hint = IsHiding ? "按 E 或 Esc 離開" : (_inRange ? "按 E 躲藏" : null);
            if (hint == null) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };
            float w = 300f, h = 36f;
            GUI.Label(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.72f, w, h), hint, style);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
