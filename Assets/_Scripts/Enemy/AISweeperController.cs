using UnityEngine;
using UnityEngine.AI;

namespace RunLight.Enemy
{
    public class AISweeperController : MonoBehaviour
    {
        [Header("巡邏路線（至少設 2 個點）")]
        [SerializeField] private Transform[] waypoints;

        [Header("速度")]
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float chaseSpeed  = 4f;

        [Header("偵測")]
        [SerializeField] private float detectRange  = 8f;
        [SerializeField] private float loseRange    = 12f;
        [SerializeField] private float catchRange   = 1.2f;
        [SerializeField] private float returnDelay  = 20f;

        [Header("2D Sprite（Billboard，可留空）")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("3D 動畫（可留空）")]
        [SerializeField] private Animator animator;

        [Header("轉身速度")]
        [SerializeField] private float turnSpeed = 8f;
        [Tooltip("模型朝向偏移（Blender 匯入方向錯時調整，試 0 / 90 / -90 / 180）")]
        [SerializeField] private float modelYOffset = 0f;

        [Header("浮空")]
        [Tooltip("離地高度")]
        [SerializeField] private float floatHeight = 1.0f;
        [Tooltip("上下飄幅度（0 = 不飄）")]
        [SerializeField] private float floatBobAmount = 0.15f;
        [Tooltip("上下飄速度")]
        [SerializeField] private float floatBobSpeed  = 1.5f;

        private enum State { Patrol, Chase, Return, Investigate }
        private State     _state = State.Patrol;
        private Vector3    _investigatePos;
        private GameObject _investigateItem;
        private float      _observeTimer;
        private int       _waypointIndex;
        private Transform _player;
        private float     _lostTimer;
        private Vector3   _returnTarget;
        private Camera    _cam;
        private Vector3   _startPosition;
        private float     _frozenTimer;
        private NavMeshAgent _agent;

        private void Start()
        {
            _cam           = Camera.main;
            _startPosition = transform.position;
            var go         = GameObject.FindWithTag("Player");
            if (go != null) _player = go.transform;
            if (animator == null) animator = GetComponentInChildren<Animator>();

            _agent = GetComponent<NavMeshAgent>();
            if (_agent != null)
            {
                _agent.speed           = patrolSpeed;
                _agent.updateRotation  = false;
                _agent.baseOffset      = floatHeight;
            }
            else
            {
                Debug.LogWarning($"[Sweeper] {name} 找不到 NavMeshAgent，會直接穿牆！");
            }
        }

        public bool IsChasing => _state == State.Chase;

        public void ResetToStart(float freezeSeconds = 0f)
        {
            if (_agent != null)
            {
                _agent.isStopped = true;
                _agent.Warp(_startPosition);
                _agent.isStopped = false;
            }
            else
            {
                transform.position = _startPosition;
            }
            _waypointIndex = 0;
            _lostTimer     = 0f;
            _frozenTimer   = freezeSeconds;
            _state         = State.Patrol;
        }

        private void Update()
        {
            BillboardSprite();

            if (_frozenTimer > 0f)
            {
                _frozenTimer -= Time.deltaTime;
                if (_agent != null) _agent.isStopped = true;
                return;
            }

            if (_agent != null) _agent.isStopped = false;

            RotateByVelocity();

            switch (_state)
            {
                case State.Patrol:      DoPatrol();      break;
                case State.Chase:       DoChase();       break;
                case State.Return:      DoReturn();      break;
                case State.Investigate: DoInvestigate(); break;
            }
        }

        private void RotateByVelocity()
        {
            if (_agent == null) return;
            var vel = _agent.velocity;
            vel.y = 0f;
            if (vel.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(vel.normalized) * Quaternion.Euler(0f, modelYOffset, 0f),
                    turnSpeed * Time.deltaTime);
            }

            // 上下飄動
            if (floatBobAmount > 0f && _agent != null)
            {
                float bob = Mathf.Sin(Time.time * floatBobSpeed) * floatBobAmount;
                _agent.baseOffset = floatHeight + bob;
            }
        }

        // 讓 Sprite 永遠面向攝影機
        private void BillboardSprite()
        {
            if (_cam == null || spriteRenderer == null) return;
            spriteRenderer.transform.LookAt(
                spriteRenderer.transform.position + _cam.transform.rotation * Vector3.forward,
                _cam.transform.rotation * Vector3.up);
        }

        private void MoveTo(Vector3 target, float speed)
        {
            if (_agent != null)
            {
                _agent.speed = speed;
                _agent.SetDestination(target);
            }
            else
            {
                // fallback（沒掛 NavMeshAgent 時）
                var dir = (target - transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    transform.position += dir.normalized * speed * Time.deltaTime;
            }
        }

        private bool ReachedTarget(Vector3 target)
        {
            if (_agent != null)
                return !_agent.pathPending && _agent.remainingDistance < 0.3f;
            return Vector3.Distance(transform.position, target) < 0.2f;
        }

        public void AlertChase()
        {
            if (_state == State.Chase) return;
            _state = State.Chase;
        }

        public static bool PlayerIsHidingStatic() => PlayerIsHiding();

        private static bool PlayerIsHiding()
        {
            if (RunLight.Interaction.BoxHideSystem.IsHiding) return true;
            foreach (var hs in FindObjectsByType<RunLight.Interaction.HidingSpot>(FindObjectsSortMode.None))
                if (hs.IsHiding) return true;
            return false;
        }

        private void DoPatrol()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            // 偵測玩家（躲藏中不追，水平距離）
            var flatDetect = _player != null ? _player.position - transform.position : Vector3.zero;
            flatDetect.y = 0f;
            if (_player != null && !PlayerIsHiding() && flatDetect.magnitude <= detectRange)
            {
                _state = State.Chase;
                return;
            }

            // 移動到下一個路徑點
            var target = waypoints[_waypointIndex].position;
            MoveTo(target, patrolSpeed);

            if (ReachedTarget(target))
                _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
        }

        private void DoChase()
        {
            if (_player == null) { _state = State.Patrol; return; }

            // 玩家躲起來就放棄追
            if (PlayerIsHiding())
            {
                _lostTimer    = returnDelay;
                _returnTarget = waypoints != null && waypoints.Length > 0
                    ? waypoints[_waypointIndex].position
                    : transform.position;
                _state = State.Return;
                return;
            }

            var flat = _player.position - transform.position;
            flat.y   = 0f;
            float dist = flat.magnitude;

            // 抓到玩家（水平距離）
            if (dist <= catchRange)
            {
                _state = State.Patrol;
                RunLight.UI.CaughtUI.TriggerCaught(this);
                return;
            }

            if (dist > loseRange)
            {
                _lostTimer    = returnDelay;
                _returnTarget = waypoints != null && waypoints.Length > 0
                    ? waypoints[_waypointIndex].position
                    : transform.position;
                _state = State.Return;
                return;
            }

            MoveTo(_player.position, chaseSpeed);
        }

        public void StartInvestigate(Vector3 pos, GameObject item)
        {
            _investigatePos  = pos;
            _investigateItem = item;
            _observeTimer    = 0f;
            _state           = State.Investigate;
        }

        private void DoInvestigate()
        {
            MoveTo(_investigatePos, patrolSpeed);

            if (!ReachedTarget(_investigatePos))
            {
                // 還沒到，繼續走
            }
            else
            {
                // 到了，開始觀察計時
                _observeTimer += Time.deltaTime;
                if (_observeTimer >= 3f)
                {
                    if (_investigateItem != null) Destroy(_investigateItem);
                    _investigateItem = null;
                    _state = State.Patrol;
                }
            }
        }

        private void DoReturn()
        {
            _lostTimer -= Time.deltaTime;
            MoveTo(_returnTarget, patrolSpeed);

            if (_lostTimer <= 0f || ReachedTarget(_returnTarget))
                _state = State.Patrol;
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Length < 2) return;
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawSphere(waypoints[i].position, 0.25f);
                var next = waypoints[(i + 1) % waypoints.Length];
                if (next != null)
                    Gizmos.DrawLine(waypoints[i].position, next.position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, loseRange);
        }
    }
}
