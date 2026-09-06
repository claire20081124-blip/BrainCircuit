using UnityEngine;
using UnityEngine.InputSystem;

namespace RunLight.Player
{
    public class CameraBobController : MonoBehaviour
    {
        [Header("走路晃動")]
        [SerializeField] private float bobSpeed   = 6f;
        [SerializeField] private float bobAmountY = 0.5f;

        [Header("快跑晃動")]
        [SerializeField] private float sprintBobSpeed   = 12f;
        [SerializeField] private float sprintBobAmountY = 0.9f;

        private Vector3 _restPos;
        private float   _timer;

        private void Start()
        {
            _restPos = transform.localPosition;
        }

        private void Update()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            bool sprinting = Keyboard.current != null && Keyboard.current.qKey.isPressed;
            float speed  = sprinting ? sprintBobSpeed   : bobSpeed;
            float amount = sprinting ? sprintBobAmountY : bobAmountY;

            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                _timer += Time.deltaTime * speed;
                transform.localPosition = _restPos + new Vector3(0f, Mathf.Sin(_timer) * amount, 0f);
            }
            else
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, _restPos, Time.deltaTime * 8f);
            }
        }
    }
}
