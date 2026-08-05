using System;
using System.Collections;
using UnityEngine;
using RunLight.Core;
using RunLight.UI;

namespace RunLight.Interaction
{
    [Serializable]
    public struct BrainQuestion
    {
        [TextArea(2, 4)]
        public string question;
        [Tooltip("正確答案：勾選=是，不勾=否")]
        public bool correctAnswer;
        [Tooltip("答對獲得的智力值")]
        public int gainAmount;
        [Tooltip("答錯損失的智力值")]
        public int loseAmount;
    }

    /// <summary>
    /// 掛在腦子物件上。玩家靠近按 E → 彈出認知測驗 → 影響智力條。
    /// 勾選 isBadBrain = 壞腦子，無論選什麼都扣分。
    /// </summary>
    public class BrainInteractable : MonoBehaviour
    {
        [Header("題目")]
        [SerializeField] private BrainQuestion question;

        [Tooltip("壞腦子：無論選哪個答案都判定錯誤")]
        [SerializeField] private bool isBadBrain;

        [Header("3D 模型")]
        [Tooltip("拖入腦子的 3D 子物件（有 MeshRenderer 的那個）")]
        [SerializeField] private GameObject brainModel;

        [Header("互動距離")]
        [SerializeField] private float interactRange = 3f;

        [Header("旋轉速度")]
        [SerializeField] private float spinSpeed = 60f;

        private static int _lastEFrame = -1; // 每幀只讓一個腦吃 E

        private Transform _player;
        private bool      _used;

        private void Start()
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) _player = go.transform;
        }

        private void Update()
        {
            if (!_used && brainModel != null)
                brainModel.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            if (_used || _player == null) return;
            if (BrainQAUI.IsOpen) return;

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist > interactRange) return;
            if (!Input.GetKeyDown(KeyCode.E)) return;
            if (_lastEFrame == Time.frameCount) return;
            _lastEFrame = Time.frameCount;
            Interact();
        }


        private void Interact()
        {
            _used = true;
            StartCoroutine(PickupRoutine());
        }

        private IEnumerator PickupRoutine()
        {
            Vector3 startScale = brainModel != null ? brainModel.transform.localScale : transform.localScale;

            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float pct = Mathf.SmoothStep(0f, 1f, t / 0.45f);
                if (brainModel != null)
                    brainModel.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, pct);
                yield return null;
            }

            if (brainModel != null) brainModel.SetActive(false);
            BrainQAUI.Instance.Show(question, isBadBrain, OnAnswered);
        }

        private void OnAnswered(bool correct)
        {
            if (PlayerStats.Instance == null) { Destroy(gameObject); return; }

            if (correct)
                PlayerStats.Instance.GainBrainPower(question.gainAmount > 0 ? question.gainAmount : 10);
            else
                PlayerStats.Instance.TakeDamage(question.loseAmount > 0 ? question.loseAmount : 8);

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
