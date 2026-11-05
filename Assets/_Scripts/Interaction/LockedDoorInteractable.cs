using UnityEngine;

namespace RunLight.Interaction
{
    public class LockedDoorInteractable : MonoBehaviour
    {
        [SerializeField] private float  interactRange = 3f;
        [SerializeField] private string lockedMessage = "鎖住了";

        private Transform _player;
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

        private void OnGUI()
        {
            if (_player == null) return;
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist > interactRange) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };

            string msg = _msgTimer > 0f ? lockedMessage : "按 E 互動";
            GUI.Label(new Rect((Screen.width - 300f) * 0.5f, Screen.height * 0.68f, 300f, 32f),
                msg, style);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
