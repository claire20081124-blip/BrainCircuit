using UnityEngine;
using UnityEngine.InputSystem;

namespace RunLight.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("移動")]
        [SerializeField] private float walkSpeed   = 3f;
        [SerializeField] private float sprintSpeed = 6f;

        [Header("視角")]
        [SerializeField] private float sensitivity = 0.15f;
        [SerializeField] private float maxPitch    = 80f;
        [Tooltip("拖入 Player 底下的 Camera 物件")]
        [SerializeField] private Transform cameraTransform;

        private CharacterController _cc;
        private Vector3 _verticalVelocity;
        private float   _pitch;
        private float   _debugTimer;
        private const float Gravity = -15f;

        private void Awake() => _cc = GetComponent<CharacterController>();

        private void Start()
        {
            // 遊戲開始時自動貼到地板，避免從高處掉落
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 100f))
            {
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            }
        }

        private void Update()
        {
            _debugTimer += Time.deltaTime;
            if (_debugTimer >= 1f)
            {
                _debugTimer = 0f;
                var kb = Keyboard.current;
                float oldH = 0f;
                try { oldH = Input.GetAxis("Horizontal"); } catch { }
                Debug.Log($"[FPS] KB={( kb != null ? "OK" : "NULL" )}  W(new)={kb?.wKey.isPressed}  H(old)={oldH:F2}  grounded={_cc.isGrounded}");
            }

            Look();
            Move();
        }

        private void Look()
        {
            // 只在有 cursor lock 時才轉視角，避免干擾測試
            if (Cursor.lockState != CursorLockMode.Locked) return;

            float dx = 0f, dy = 0f;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var d = mouse.delta.ReadValue();
                dx = d.x; dy = d.y;
            }
            try { dx += Input.GetAxis("Mouse X"); dy += Input.GetAxis("Mouse Y"); } catch { }

            transform.Rotate(Vector3.up * dx * sensitivity);
            _pitch -= dy * sensitivity;
            _pitch  = Mathf.Clamp(_pitch, -maxPitch, maxPitch);
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void Move()
        {
            float h = 0f, v = 0f;

            // 新版 Input System
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed) v += 1f;
                if (kb.sKey.isPressed) v -= 1f;
                if (kb.dKey.isPressed) h += 1f;
                if (kb.aKey.isPressed) h -= 1f;
            }

            // 舊版備援
            try
            {
                h += Input.GetAxis("Horizontal");
                v += Input.GetAxis("Vertical");
            }
            catch { }

            // 重力
            if (_cc.isGrounded && _verticalVelocity.y < 0f)
                _verticalVelocity.y = -2f;
            _verticalVelocity.y += Gravity * Time.deltaTime;

            bool sprinting = kb != null && kb.qKey.isPressed;
            float speed = sprinting ? sprintSpeed : walkSpeed;

            var move = transform.right * h + transform.forward * v;
            _cc.Move((move * speed + _verticalVelocity) * Time.deltaTime);
        }

        /// <summary>強制設定玩家朝向（供躲藏點等機制呼叫）。</summary>
        public void ForceRotation(float yaw, float pitch)
        {
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            _pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        // 點擊 Game 視窗鎖定游標；Escape 解鎖
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void OnGUI()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                GUI.Label(new Rect(10, 10, 400, 30), "點擊畫面鎖定游標並開始移動");
                if (Event.current.type == EventType.MouseDown)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible   = false;
                }
            }
        }
    }
}
