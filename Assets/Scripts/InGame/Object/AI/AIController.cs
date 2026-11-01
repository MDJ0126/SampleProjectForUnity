using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    /// <summary>
    /// Behavior 그래프가 요청하는 탐색·접근·공격을 공통 Pawn에 연결한다.
    /// </summary>
    [RequireComponent(typeof(BehaviorGraphAgent), typeof(NavMeshAgent))]
    public class AIController : PawnController
    {
        public Pawn controlledPawn;
        public float detectionRange = 25f;
        private NavMeshAgent _agent;
        private BehaviorGraphAgent _graph;
        private float _pathRequestedAt;
        private float _progressAt;
        private Vector3 _progressPosition;
        public bool HasPath => _agent != null && _agent.isOnNavMesh && _agent.hasPath;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _graph = GetComponent<BehaviorGraphAgent>();
            _agent.updatePosition = false;
            _agent.updateRotation = false;
        }

        private void Start()
        {
            Possess(controlledPawn);
        }

        private void Update()
        {
            if (possessTarget == null || possessTarget.IsDead || !possessTarget.IsCombatEnabled)
            {
                StopMove();
                return;
            }
            if (_agent.isOnNavMesh)
            {
                _agent.nextPosition = possessTarget.transform.position;
                possessTarget.Movement.MoveInput = _agent.desiredVelocity.sqrMagnitude > 0.01f ? _agent.desiredVelocity.normalized : Vector3.zero;
            }
        }

        private void OnDisable()
        {
            StopMove();
        }

        /// <summary>
        /// 컨트롤러가 다시 빙의하면 Behavior 실행을 처음부터 시작한다.
        /// </summary>
        protected override void OnPossessionChanged()
        {
            if (_graph != null && _graph.Graph != null && IsPossessed)
            {
                _graph.Restart();
            }
            StopMove();
        }

        /// <summary>
        /// 탐지 범위 안에서 가장 가까운 유효한 적을 선택한다.
        /// </summary>
        public Pawn FindTarget()
        {
            if (possessTarget == null || !possessTarget.IsCombatEnabled || possessTarget.IsDead)
            {
                return null;
            }
            Pawn nearest = null;
            float distance = detectionRange * detectionRange;
            foreach (Pawn pawn in Pawn.ActivePawns)
            {
                float candidate = (pawn.transform.position - possessTarget.transform.position).sqrMagnitude;
                if (possessTarget.CanDamage(pawn) && candidate < distance)
                {
                    nearest = pawn;
                    distance = candidate;
                }
            }
            return nearest;
        }

        /// <summary>
        /// 목표와 무기 사거리를 검사하고 도달할 수 없는 경로를 실패시킨다.
        /// </summary>
        public Node.Status Approach(Pawn target)
        {
            Pawn pawn = possessTarget;
            if (pawn == null || !pawn.CanDamage(target) || pawn.EquippedWeapon == null || !_agent.isOnNavMesh)
            {
                StopMove();
                return Node.Status.Failure;
            }
            Vector3 direction = target.AimPoint - pawn.AimPoint;
            pawn.Movement.FacingDirection = direction;
            if (aimTarget != null && aimTarget.target != null)
            {
                aimTarget.target.position = target.AimPoint;
            }
            float range = pawn.EquippedWeapon is Gun ? Mathf.Min(pawn.EquippedWeapon.range, 12f) : pawn.EquippedWeapon.range;
            bool visible = !GameUtils.TryRaycast(new Ray(pawn.AimPoint, direction.normalized), direction.magnitude, ~0, pawn, out RaycastHit hit) || hit.collider.GetComponentInParent<Pawn>() == target;
            if (direction.magnitude <= range && visible)
            {
                _progressAt = 0f;
                StopMove();
                pawn.Movement.FacingDirection = direction;
                return Node.Status.Success;
            }
            if (_progressAt == 0f || (pawn.transform.position - _progressPosition).sqrMagnitude > 0.04f)
            {
                _progressAt = Time.time;
                _progressPosition = pawn.transform.position;
            }
            else if (Time.time - _progressAt > 3f)
            {
                _progressAt = 0f;
                StopMove();
                return Node.Status.Failure;
            }
            if (Time.time >= _pathRequestedAt)
            {
                _pathRequestedAt = Time.time + 0.3f;
                if (!_agent.SetDestination(target.transform.position))
                {
                    return Node.Status.Failure;
                }
            }
            if (!_agent.pathPending && _agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                return Node.Status.Failure;
            }
            _agent.isStopped = false;
            return Node.Status.Running;
        }

        /// <summary>
        /// 사거리 유지 중 같은 공격 API를 호출하며 빈 탄창은 장전한다.
        /// </summary>
        public Node.Status Attack(Pawn target)
        {
            if (Approach(target) != Node.Status.Success)
            {
                return Node.Status.Failure;
            }
            Pawn pawn = possessTarget;
            pawn.SetWeaponDrawn(true);
            if (pawn.EquippedWeapon is Gun gun && gun.Ammo == 0)
            {
                if (gun.Reserve == 0)
                {
                    pawn.SelectWeapon(0);
                    return Node.Status.Failure;
                }
                gun.Reload();
            }
            pawn.TryAttack(target.AimPoint);
            return Node.Status.Running;
        }

        /// <summary>
        /// 경로와 이동 입력을 함께 정지한다.
        /// </summary>
        public void StopMove()
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
            possessTarget?.Movement?.StopMove();
        }
    }
}
