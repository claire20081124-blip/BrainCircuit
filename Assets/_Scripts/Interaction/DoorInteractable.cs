using UnityEngine;
using RunLight.Core;

namespace RunLight.Interaction
{
    /// <summary>
    /// 掛在門物件上。玩家靠近按 E 開門，開門時自動存檔一次。
    /// </summary>
    public class DoorInteractable : MonoBehaviour
    {
        [Header("開門動畫")]
        [Tooltip("開門旋轉角度（Y 軸，正值往右開、負值往左開）")]
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float openSpeed = 4f;

        [Header("互動距離")]
        [SerializeField] private float interactRange = 3f;

        private Quaternion _closedRot;
        private Quaternion _openRot;
        private bool       _isOpen;
        private bool       _saved;
        private Transform  _player;

        private void Start()
        {
            _closedRot = transform.localRotation;
            _openRot   = _closedRot * Quaternion.Euler(0f, openAngle, 0f);

            var go = GameObject.FindWithTag("Player");
            if (go != null) _player = go.transform;
        }

        private void Update()
        {
            if (_player == null) return;

            float dist = Vector3.Distance(transform.position, _player.position);

            if (!_isOpen && Input.GetKeyDown(KeyCode.E) && dist <= interactRange)
                Open();

            // 平滑旋轉
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                _isOpen ? _openRot : _closedRot,
                Time.deltaTime * openSpeed);
        }

        private void Open()
        {
            _isOpen = true;

            if (!_saved)
            {
                _saved = true;
                if (GameManager.HasInstance)
                    GameManager.Instance.SaveGame();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
