using UnityEngine;
using RunLight.Enemy;
using RunLight.Inventory;

namespace RunLight.Interaction
{
    public class ThrownItem : MonoBehaviour
    {
        [SerializeField] private float distractionRange = 12f;

        public InventoryItem sourceItem;

        private bool _landed;

        private void OnCollisionEnter(Collision col)
        {
            if (_landed) return;
            _landed = true;

            // 通知範圍內所有清道夫過來調查
            var sweepers = FindObjectsByType<AISweeperController>(FindObjectsSortMode.None);
            foreach (var s in sweepers)
            {
                if (Vector3.Distance(transform.position, s.transform.position) <= distractionRange)
                    s.StartInvestigate(transform.position, gameObject);
            }

            // 沒有清道夫在範圍內就自行延遲銷毀
            if (sweepers.Length == 0)
                Destroy(gameObject, 10f);

            // 停止物理
            var rb = GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }
        }
    }
}
