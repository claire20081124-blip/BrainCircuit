using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
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
        [Tooltip("限制作答時間（秒），0 = 使用預設 10 秒")]
        public float timeLimit;
    }

    public class BrainInteractable : MonoBehaviour
    {
        [Header("題目")]
        [SerializeField] private BrainQuestion question;

        [Tooltip("壞腦子：無論選哪個答案都判定錯誤")]
        [SerializeField] private bool isBadBrain;

        [Header("3D 模型")]
        [Tooltip("拖入腦子的 3D 子物件（有 MeshRenderer 的那個）")]
        [SerializeField] private GameObject brainModel;

        [Header("音效")]
        [SerializeField] private AudioClip correctSound;
        [SerializeField] private AudioClip wrongSound;
        [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;

        [Header("互動距離")]
        [SerializeField] private float interactRange = 8f;

        [Header("旋轉速度")]
        [SerializeField] private float spinSpeed = 60f;

        [Header("定點移動")]
        [SerializeField] private float moveSpeed   = 1.5f;
        [SerializeField] private float waitAtPoint = 1.5f;

        private Transform[]  _movePoints;
        private int          _moveTarget;
        private float        _waitTimer;
        private NavMeshAgent _agent;

        // 每幀最近的腦子才能觸發（Update 投票，LateUpdate 執行）
        private static BrainInteractable _candidate;
        private static float             _candidateDist;
        private static int               _candidateFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _candidate      = null;
            _candidateDist  = float.MaxValue;
            _candidateFrame = -1;
        }

        private Transform   _player;
        private bool        _used;
        private AudioSource _audio;

        public void Init(BrainQuestion q, bool bad = false, Transform[] movePoints = null)
        {
            question    = q;
            isBadBrain  = bad;
            _movePoints = movePoints;
            _moveTarget = movePoints != null && movePoints.Length > 1
                ? UnityEngine.Random.Range(0, movePoints.Length) : 0;
        }

        private void Awake()
        {
            if (brainModel == null)
            {
                var mr = GetComponentInChildren<MeshRenderer>();
                if (mr != null) brainModel = mr.gameObject;
            }
        }

        private void Start()
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) _player = go.transform;

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 0f;
            _audio.playOnAwake  = false;
            _audio.volume       = soundVolume;

            _agent = GetComponent<NavMeshAgent>();
            if (_agent != null)
            {
                _agent.speed          = moveSpeed;
                _agent.updateRotation = false;
            }
        }

        private void Update()
        {
            if (!_used && brainModel != null)
                brainModel.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            if (!_used && _movePoints != null && _movePoints.Length > 1)
                MoveBetweenPoints();

            if (_player == null)
            {
                var go = GameObject.FindWithTag("Player");
                if (go != null) _player = go.transform;
            }

            // 用球體的實際世界位置算距離，而非 Root 的 pivot
            Vector3 center = brainModel != null ? brainModel.transform.position : transform.position;

            if (_used || _player == null) return;

            float dist = Vector3.Distance(center, _player.position);
            if (dist > interactRange) return;
            if (!Input.GetKeyDown(KeyCode.E)) return;
            if (BrainQAUI.IsOpen) return;
            if (CodeDoorInteractable.HasCandidate) return;

            // 投票：距離最近的腦子成為本幀候選
            if (_candidateFrame != Time.frameCount)
            {
                _candidateFrame = Time.frameCount;
                _candidate      = null;
                _candidateDist  = float.MaxValue;
            }
            if (dist < _candidateDist)
            {
                _candidateDist = dist;
                _candidate     = this;
            }
        }

        private void LateUpdate()
        {
            if (_candidate == this && _candidateFrame == Time.frameCount)
                Interact();
        }

        private void Interact()
        {
            _used = true;
            BrainQAUI.Lock();
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

            var clip = correct ? correctSound : wrongSound;
            if (clip != null && _audio != null)
                AudioSource.PlayClipAtPoint(clip, _player != null ? _player.position : transform.position, soundVolume);

            if (correct)
                PlayerStats.Instance.GainBrainPower(Mathf.Max(1, (question.gainAmount > 0 ? question.gainAmount : 10) / 2));
            else
                PlayerStats.Instance.TakeDamage(Mathf.Max(1, (question.loseAmount > 0 ? question.loseAmount : 8) / 2));

            Destroy(gameObject);
        }

        private void MoveBetweenPoints()
        {
            Vector3 target = _movePoints[_moveTarget].position;

            bool arrived;
            if (_agent != null)
            {
                _agent.speed = moveSpeed;
                _agent.SetDestination(target);
                arrived = !_agent.pathPending && _agent.remainingDistance < 0.3f;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                arrived = Vector3.Distance(transform.position, target) < 0.1f;
            }

            if (arrived)
            {
                _waitTimer += Time.deltaTime;
                if (_waitTimer >= waitAtPoint)
                {
                    _waitTimer  = 0f;
                    _moveTarget = (_moveTarget + 1) % _movePoints.Length;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
