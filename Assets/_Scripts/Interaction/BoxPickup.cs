using UnityEngine;

namespace RunLight.Interaction
{
    /// <summary>
    /// 放在迷宮裡的紙箱物件。走近按 E 撿起，撿起後 BoxHideSystem 解鎖 Space 躲藏功能。
    /// </summary>
    public class BoxPickup : MonoBehaviour
    {
        [SerializeField] private float interactRange = 2.5f;
        [SerializeField] private string promptText   = "按 E 撿起紙箱";

        private Transform _player;
        private bool      _pickedUp;

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
            if (_pickedUp || _player == null) return;
            if (Vector3.Distance(transform.position, _player.position) <= interactRange
                && Input.GetKeyDown(KeyCode.E))
                DoPickup();
        }

        private void DoPickup()
        {
            _pickedUp = true;
            BoxHideSystem.Instance?.PickUpBox();
            Destroy(gameObject);
        }

        private void OnGUI()
        {
            if (_pickedUp || _player == null) return;
            if (Vector3.Distance(transform.position, _player.position) > interactRange) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 18,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(1f, 1f, 1f, 0.85f) }
            };
            GUI.Label(new Rect((Screen.width - 300f) * 0.5f, Screen.height * 0.72f, 300f, 28f),
                promptText, style);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
