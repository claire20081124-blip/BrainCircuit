using UnityEngine;
using UnityEngine.InputSystem;
using RunLight.UI;

namespace RunLight.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("移動")]
        [SerializeField] private float walkSpeed   = 3f;
        [SerializeField] private float sprintSpeed = 6f;

        [Header("體力")]
        [SerializeField] private float maxStamina        = 100f;
        [SerializeField] private float staminaDrainRate  = 25f;   // 每秒消耗
        [SerializeField] private float staminaRegenRate  = 35f;   // 每秒回復
        [SerializeField] private float slowdownThreshold = 0.3f;  // 低於此比例開始降速

        [Header("視角")]
        [SerializeField] private float sensitivity = 0.15f;
        [SerializeField] private float maxPitch    = 80f;
        [Tooltip("拖入 Player 底下的 Camera 物件")]
        [SerializeField] private Transform cameraTransform;

        // Tab 游標切換已移至 BackpackUI

        public float StaminaRatio   => _stamina / maxStamina;
        public bool  IsSprinting    { get; private set; }
        public bool  MovementLocked { get; set; }
        public float Pitch          => _pitch;

        private CharacterController _cc;
        private Vector3 _verticalVelocity;
        private float   _pitch;
        private float   _stamina;
        private bool    _exhausted;
        private const float Gravity = -15f;

        private void Awake() => _cc = GetComponent<CharacterController>();

        private void Start()
        {
            _stamina = maxStamina;

            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 100f))
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }

        private void Update()
        {
            if (MovementLocked) return;

            Look();
            Move();
        }

        private void ToggleCursor()
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible   = locked;
        }

        private void Look()
        {
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

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed) v += 1f;
                if (kb.sKey.isPressed) v -= 1f;
                if (kb.dKey.isPressed) h += 1f;
                if (kb.aKey.isPressed) h -= 1f;
            }
            try { h += Input.GetAxis("Horizontal"); v += Input.GetAxis("Vertical"); } catch { }

            if (_cc.isGrounded && _verticalVelocity.y < 0f)
                _verticalVelocity.y = -2f;
            _verticalVelocity.y += Gravity * Time.deltaTime;

            bool moving     = Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f;
            bool wantSprint = kb != null && kb.qKey.isPressed;

            // 體力消耗 / 回復
            if (wantSprint && moving && !_exhausted && _stamina > 0f)
            {
                _stamina -= staminaDrainRate * Time.deltaTime;
                if (_stamina <= 0f) { _stamina = 0f; _exhausted = true; }
            }
            else
            {
                _stamina += staminaRegenRate * Time.deltaTime;
                if (_stamina >= maxStamina) _stamina = maxStamina;
                if (_exhausted && _stamina >= maxStamina * 0.25f) _exhausted = false;
            }

            IsSprinting = wantSprint && moving && !_exhausted && _stamina > 0f;

            float speed;
            if (IsSprinting)
            {
                float ratio = _stamina / maxStamina;
                speed = ratio < slowdownThreshold
                    ? Mathf.Lerp(walkSpeed, sprintSpeed, ratio / slowdownThreshold)
                    : sprintSpeed;
            }
            else
            {
                speed = walkSpeed;
            }

            var move = transform.right * h + transform.forward * v;
            _cc.Move((move * speed + _verticalVelocity) * Time.deltaTime);
        }

        public void SetSensitivity(float s) => sensitivity = s;
        public float GetSensitivity()        => sensitivity;

        public void ForceRotation(float yaw, float pitch)
        {
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            _pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

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
            if (MovementLocked) return;
            if (UI.BrainQAUI.IsOpen) return;

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
