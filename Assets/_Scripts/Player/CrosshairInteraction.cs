using UnityEngine;
using UnityEngine.UI;
using RunLight.Interaction;

namespace RunLight.Player
{
    public class CrosshairInteraction : MonoBehaviour
    {
        [SerializeField] private float  interactRange   = 4f;
        [SerializeField] private Color  crosshairColor  = new Color(1f, 1f, 1f, 0.75f);
        [SerializeField] private Color  crosshairActive = new Color(1f, 0.85f, 0.3f, 1f);

        private Camera _cam;
        private Text   _crosshairTxt;
        private Text   _hintTxt;

        private void Start()
        {
            _cam = GetComponentInChildren<Camera>();
            if (_cam == null) _cam = Camera.main;
            BuildHUD();
        }

        private void Update()
        {
            if (_cam == null) return;

            bool uiOpen = UI.BackpackUI.IsOpen
                       || ThrowSystem.IsAiming
                       || NoteReadable.IsReading
                       || CodeDoorInteractable.IsInputting;

            if (uiOpen)
            {
                SetHint(""); SetCrosshairActive(false); return;
            }

            var ray = new Ray(_cam.transform.position, _cam.transform.forward);
            if (Physics.Raycast(ray, out var hit, interactRange))
            {
                // 丟出的道具
                var thrown = hit.collider.GetComponentInParent<ThrownItem>();
                if (thrown != null && thrown.CanPickup)
                {
                    SetHint($"E  撿回 {thrown.ItemName}");
                    SetCrosshairActive(true);
                    if (Input.GetKeyDown(KeyCode.E)) thrown.DoPickup();
                    return;
                }

                // 地上可撿的道具
                var pickup = hit.collider.GetComponentInParent<WorldPickup>();
                if (pickup != null && pickup.CanPickup)
                {
                    SetHint($"E  撿起 {pickup.DisplayName}");
                    SetCrosshairActive(true);
                    if (Input.GetKeyDown(KeyCode.E)) pickup.DoPickup();
                    return;
                }
            }

            SetHint("");
            SetCrosshairActive(false);
        }

        private void SetHint(string msg)
        {
            if (_hintTxt != null) _hintTxt.text = msg;
        }

        private void SetCrosshairActive(bool active)
        {
            if (_crosshairTxt != null)
                _crosshairTxt.color = active ? crosshairActive : crosshairColor;
        }

        private void BuildHUD()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("CrosshairCanvas",
                typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            // 準心（+）
            var cGo = new GameObject("Crosshair", typeof(Text));
            cGo.transform.SetParent(canvasGo.transform, false);
            _crosshairTxt = cGo.GetComponent<Text>();
            _crosshairTxt.font      = font;
            _crosshairTxt.text      = "+";
            _crosshairTxt.fontSize  = 48;
            _crosshairTxt.color     = crosshairColor;
            _crosshairTxt.alignment = TextAnchor.MiddleCenter;
            _crosshairTxt.raycastTarget = false;
            var crt = cGo.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(80f, 80f);
            crt.anchoredPosition = Vector2.zero;

            // 互動提示（準心下方）
            var hGo = new GameObject("Hint", typeof(Text));
            hGo.transform.SetParent(canvasGo.transform, false);
            _hintTxt = hGo.GetComponent<Text>();
            _hintTxt.font      = font;
            _hintTxt.text      = "";
            _hintTxt.fontSize  = 20;
            _hintTxt.color     = new Color(1f, 1f, 1f, 0.9f);
            _hintTxt.fontStyle = FontStyle.Bold;
            _hintTxt.alignment = TextAnchor.MiddleCenter;
            _hintTxt.raycastTarget = false;
            var hrt = hGo.GetComponent<RectTransform>();
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f);
            hrt.sizeDelta        = new Vector2(500f, 40f);
            hrt.anchoredPosition = new Vector2(0f, -50f);
        }
    }
}
