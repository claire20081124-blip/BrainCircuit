using System.Collections;
using UnityEngine;
using RunLight.Inventory;

namespace RunLight.Interaction
{
    public class WorldPickup : MonoBehaviour
    {
        [Header("道具資訊")]
        [SerializeField] private string    itemId          = "item_001";
        [SerializeField] private string    displayName     = "道具";
        [SerializeField] private string    description     = "說明文字";
        [SerializeField] private ItemType  itemType        = ItemType.Other;
        [SerializeField] private Color     placeholderColor = Color.cyan;

        [Header("互動")]
        [SerializeField] private float interactRange = 3f;

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
            {
                Pickup();
            }
        }

        private void Pickup()
        {
            _pickedUp = true;
            StartCoroutine(PickupRoutine());
        }

        private IEnumerator PickupRoutine()
        {
            var startScale = transform.localScale;
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float ratio = Mathf.SmoothStep(0f, 1f, t / 0.4f);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, ratio);
                // 同時往上漂
                transform.position  += Vector3.up * Time.deltaTime * 2f;
                yield return null;
            }

            var item = new InventoryItem
            {
                id               = itemId + "_" + System.Guid.NewGuid().ToString("N")[..6],
                displayName      = displayName,
                description      = description,
                type             = itemType,
                placeholderColor = placeholderColor
            };

            InventorySystem.Instance?.Add(item);
            Destroy(gameObject);
        }

        private void OnGUI()
        {
            if (_pickedUp || _player == null) return;
            if (Vector3.Distance(transform.position, _player.position) > interactRange) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };
            float w = 300f, h = 32f;
            GUI.Label(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.68f, w, h),
                $"按 E 撿起 [{displayName}]", style);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
