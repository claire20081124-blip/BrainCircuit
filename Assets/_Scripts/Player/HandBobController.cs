using UnityEngine;
using UnityEngine.InputSystem;

namespace RunLight.Player
{
    public class HandBobController : MonoBehaviour
    {
        [Header("手部 RectTransform")]
        [SerializeField] private RectTransform leftHand;
        [SerializeField] private RectTransform rightHand;

        [Header("動畫")]
        [SerializeField] private float bobSpeed    = 9f;
        [SerializeField] private float bobAmountY  = 40f;
        [SerializeField] private float bobAmountX  = 12f;
        [SerializeField] private float bobRotation = 6f;
        [SerializeField] private float returnSpeed = 8f;

        private Vector2 _leftRest;
        private Vector2 _rightRest;
        private float   _timer;

        private void Start()
        {
            if (leftHand  != null) _leftRest  = leftHand.anchoredPosition;
            if (rightHand != null) _rightRest = rightHand.anchoredPosition;
        }

        private void Update()
        {
            bool moving = IsMoving();

            if (moving)
            {
                _timer += Time.deltaTime * bobSpeed;

                float sin = Mathf.Sin(_timer);
                float cos = Mathf.Cos(_timer);

                if (leftHand != null)
                {
                    leftHand.anchoredPosition = _leftRest + new Vector2(
                        cos * bobAmountX,
                        sin * bobAmountY);
                    leftHand.localRotation = Quaternion.Euler(0f, 0f, sin * bobRotation);
                }

                if (rightHand != null)
                {
                    // 右手與左手反相（sin + π）
                    rightHand.anchoredPosition = _rightRest + new Vector2(
                        -cos * bobAmountX,
                        -sin * bobAmountY);
                    rightHand.localRotation = Quaternion.Euler(0f, 0f, -sin * bobRotation);
                }
            }
            else
            {
                float t = Time.deltaTime * returnSpeed;
                if (leftHand != null)
                {
                    leftHand.anchoredPosition = Vector2.Lerp(leftHand.anchoredPosition, _leftRest, t);
                    leftHand.localRotation    = Quaternion.Lerp(leftHand.localRotation, Quaternion.identity, t);
                }
                if (rightHand != null)
                {
                    rightHand.anchoredPosition = Vector2.Lerp(rightHand.anchoredPosition, _rightRest, t);
                    rightHand.localRotation    = Quaternion.Lerp(rightHand.localRotation, Quaternion.identity, t);
                }
            }
        }

        private static bool IsMoving()
        {
            var kb = Keyboard.current;
            if (kb != null &&
                (kb.wKey.isPressed || kb.sKey.isPressed ||
                 kb.aKey.isPressed || kb.dKey.isPressed))
                return true;

            try
            {
                if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f) return true;
                if (Mathf.Abs(Input.GetAxis("Vertical"))   > 0.1f) return true;
            }
            catch { }

            return false;
        }
    }
}
