using UnityEngine;
using RunLight.Enemy;
using RunLight.Inventory;

namespace RunLight.Interaction
{
    public class ThrownItem : MonoBehaviour
    {
        [SerializeField] private float distractionRange = 12f;
        [SerializeField] private float pickupRange      = 3.5f;

        public InventoryItem sourceItem;

        private bool      _landed;
        private Transform _player;

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

        private void Update() { }

        public bool   CanPickup => _notified;
        public string ItemName  => sourceItem?.displayName ?? "道具";

        public void DoPickup()
        {
            InventorySystem.Instance?.Add(sourceItem);
            Destroy(gameObject);
        }

        private void Pickup() => DoPickup();

        private bool _notified;

        private void OnCollisionEnter(Collision col)
        {
            // 第一次碰撞：立刻通知清道夫（不管打到牆還是地）
            if (!_notified)
            {
                _notified = true;
                var sweepers = FindObjectsByType<AISweeperController>(FindObjectsSortMode.None);
                foreach (var s in sweepers)
                {
                    if (Vector3.Distance(transform.position, s.transform.position) <= distractionRange)
                        s.StartInvestigate(transform.position, gameObject);
                }
                if (sweepers.Length == 0)
                    Destroy(gameObject, 10f);
            }

            // 打到地板才算落地（讓玩家可以撿起，並開始等待靜止）
            if (_landed) return;
            foreach (var contact in col.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    _landed = true;
                    StartCoroutine(FreezeWhenSettled());
                    break;
                }
            }
        }

        private System.Collections.IEnumerator FreezeWhenSettled()
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null) yield break;

            // 最多等 3 秒，等速度夠小再鎖定
            float timer = 0f;
            while (timer < 3f)
            {
                if (rb.linearVelocity.sqrMagnitude < 0.01f && rb.angularVelocity.sqrMagnitude < 0.01f)
                    break;
                timer += Time.deltaTime;
                yield return null;
            }

            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }
        }

        private void OnGUI()
        {
            if (!_notified || _player == null) return;
            if (Vector3.Distance(transform.position, _player.position) > pickupRange) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 20, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };
            string name = sourceItem != null ? sourceItem.displayName : "道具";
            GUI.Label(new Rect((Screen.width - 300f) * 0.5f, Screen.height * 0.68f, 300f, 32f),
                $"按 E 撿回 [{name}]", style);
        }
    }
}
