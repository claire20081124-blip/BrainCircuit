using UnityEngine;

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
        [SerializeField] private float returnDelay  = 20f;

        [Header("2D Sprite（Billboard）")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private enum State { Patrol, Chase, Return }
        private State     _state = State.Patrol;
        private int       _waypointIndex;
        private Transform _player;
        private float     _lostTimer;
        private Vector3   _returnTarget;
        private Camera    _cam;

        private void Start()
        {
            _cam    = Camera.main;
            var go  = GameObject.FindWithTag("Player");
            if (go != null) _player = go.transform;
        }

        private void Update()
        {
            BillboardSprite();

            switch (_state)
            {
                case State.Patrol: DoPatrol(); break;
                case State.Chase:  DoChase();  break;
                case State.Return: DoReturn(); break;
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

        private static bool PlayerIsHiding()
        {
            foreach (var hs in FindObjectsByType<RunLight.Interaction.HidingSpot>(FindObjectsSortMode.None))
                if (hs.IsHiding) return true;
            return false;
        }

        private void DoPatrol()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            // 偵測玩家（躲藏中不追）
            if (_player != null && !PlayerIsHiding() &&
                Vector3.Distance(transform.position, _player.position) <= detectRange)
            {
                _state = State.Chase;
                return;
            }

            // 移動到下一個路徑點
            var target = waypoints[_waypointIndex].position;
            transform.position = Vector3.MoveTowards(transform.position, target, patrolSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) < 0.2f)
                _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
        }

        private void DoChase()
        {
            if (_player == null) { _state = State.Patrol; return; }

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist > loseRange)
            {
                _lostTimer    = returnDelay;
                _returnTarget = waypoints != null && waypoints.Length > 0
                    ? waypoints[_waypointIndex].position
                    : transform.position;
                _state = State.Return;
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position, _player.position, chaseSpeed * Time.deltaTime);
        }

        private void DoReturn()
        {
            _lostTimer -= Time.deltaTime;
            transform.position = Vector3.MoveTowards(
                transform.position, _returnTarget, patrolSpeed * Time.deltaTime);

            if (_lostTimer <= 0f || Vector3.Distance(transform.position, _returnTarget) < 0.2f)
                _state = State.Patrol;
        }

        // 在 Scene 視窗顯示偵測範圍
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, loseRange);
        }
    }
}
