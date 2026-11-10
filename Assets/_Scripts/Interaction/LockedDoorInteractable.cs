using UnityEngine;

namespace RunLight.Interaction
{
    public class LockedDoorInteractable : MonoBehaviour
    {
        [SerializeField] private float  interactRange = 3f;
        [SerializeField] private string lockedMessage = "鎖住了";

        private Transform _player;
        private Camera    _cam;
        private float     _msgTimer;

        private void Start()
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) _player = go.transform;

            if (_player == null)
            {
                var fpc = FindObjectOfType<RunLight.Player.FirstPersonController>();
                if (fpc != null) _player = fpc.transform;
            }

            var fpcForCam = FindObjectOfType<RunLight.Player.FirstPersonController>();
            _cam = fpcForCam != null ? fpcForCam.GetComponentInChildren<Camera>() : null;
            if (_cam == null) _cam = Camera.main;
        }

        private void Update()
        {
            if (_player == null) return;
            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist <= interactRange && Input.GetKeyDown(KeyCode.E)
                && !CodeDoorInteractable.IsInputting
                && !NoteReadable.IsReading)
            {
                _msgTimer = 2f;
            }

            if (_msgTimer > 0f) _msgTimer -= Time.deltaTime;
        }

        private void LateUpdate()
        {
            if (_player == null || _cam == null) return;
            if (Vector3.Distance(transform.position, _player.position) > interactRange) return;
            float angle = Vector3.Angle(_cam.transform.forward,
                (transform.position - _cam.transform.position).normalized);
            if (angle > 70f) return;
            string msg = _msgTimer > 0f ? lockedMessage : "按 E 互動";
            InteractionHint.Submit(msg, 8);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
