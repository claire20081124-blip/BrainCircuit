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
        [SerializeField] private float interactRange = 5f;

        public bool IsHiding { get; private set; }

        private Transform            _player;
        private CharacterController  _cc;
        private FirstPersonController _fpc;

        private bool  _doorOpen;
        private float _leftAngle;
        private float _rightAngle;
        private Quaternion _leftClosed;
        private Quaternion _rightClosed;

        private void Start()
        {
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo == null) return;
            _player = playerGo.transform;
            _cc     = playerGo.GetComponent<CharacterController>();
            _fpc    = playerGo.GetComponent<FirstPersonController>();

            if (leftDoor  != null) _leftClosed  = leftDoor.localRotation;
            if (rightDoor != null) _rightClosed = rightDoor.localRotation;
        }

        private void Update()
        {
            if (_player == null)
            {
                var go = GameObject.FindWithTag("Player");
                if (go != null)
                {
                    _player = go.transform;
                    _cc     = go.GetComponent<CharacterController>();
                    _fpc    = go.GetComponent<FirstPersonController>();
                }
                else return;
            }

            float dist = Vector3.Distance(transform.position, _player.position);

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (IsHiding)             ExitHide();
                else if (dist <= interactRange) TryHide();
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

            if (_cc != null) _cc.enabled = false;
            _player.position = hidePosition.position;

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
            if (_cc != null) _cc.enabled = false;
            // 傳到出口位置，再開 CC
            var dest = exitPosition != null ? exitPosition.position : transform.position;
            _player.position = dest;
            if (_cc != null) _cc.enabled = true;
        }

        private void AnimateDoors()
        {
            if (leftDoor != null)
            {
                var openRot  = _leftClosed * Quaternion.Euler(0f, -doorOpenAngle, 0f);
                var closeRot = _leftClosed;
                leftDoor.localRotation = Quaternion.Slerp(
                    leftDoor.localRotation,
                    _doorOpen ? openRot : closeRot,
                    Time.deltaTime * doorSpeed);
            }

            if (rightDoor != null)
            {
                var openRot  = _rightClosed * Quaternion.Euler(0f, doorOpenAngle, 0f);
                var closeRot = _rightClosed;
                rightDoor.localRotation = Quaternion.Slerp(
                    rightDoor.localRotation,
                    _doorOpen ? openRot : closeRot,
                    Time.deltaTime * doorSpeed);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
