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

        [Header("音效")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;

        [Header("互動距離")]
        [SerializeField] private float interactRange = 3f;

        [Header("門口擋牆（開門時關閉）")]
        [SerializeField] private Collider doorBlocker;

        private Quaternion  _closedRot;
        private Quaternion  _openRot;
        private bool        _isOpen;
        private bool        _saved;
        private Transform   _player;
        private AudioSource _audio;

        private void Start()
        {
            _closedRot = transform.localRotation;
            _openRot   = _closedRot * Quaternion.Euler(0f, openAngle, 0f);

            var go = GameObject.FindWithTag("Player");
            if (go != null) _player = go.transform;

            if (_player == null)
            {
                var fpc = FindObjectOfType<RunLight.Player.FirstPersonController>();
                if (fpc != null) _player = fpc.transform;
            }

            if (_player == null) Debug.LogWarning("[Door] 找不到玩家！");

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 0f;
            _audio.playOnAwake  = false;
            _audio.volume       = soundVolume;
        }

        private void Update()
        {
            if (_player == null) return;

            float dist = Vector3.Distance(transform.position, _player.position);

            if (Input.GetKeyDown(KeyCode.E) && dist <= interactRange)
            {
                if (_isOpen) Close();
                else Open();
            }

            // 平滑旋轉
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                _isOpen ? _openRot : _closedRot,
                Time.deltaTime * openSpeed);
        }

        private void Open()
        {
            _isOpen = true;
            if (doorBlocker != null) doorBlocker.enabled = false;
            if (openSound != null) { _audio.clip = openSound; _audio.Play(); Debug.Log("[Door] 播放開門音效"); }
            else Debug.LogWarning("[Door] openSound 是空的！");

            if (!_saved)
            {
                _saved = true;
                if (GameManager.HasInstance)
                    GameManager.Instance.SaveGame();
            }
        }

        private void Close()
        {
            _isOpen = false;
            if (doorBlocker != null) doorBlocker.enabled = true;
            if (closeSound != null) { _audio.clip = closeSound; _audio.Play(); }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
