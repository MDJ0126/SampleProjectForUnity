using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 공격·피격 충돌을 처리하고 선택적으로 프레임 사이 이동·회전 검사와 디버그 표시를 수행한다.
    /// </summary>
    [DefaultExecutionOrder(31000)]
    public class HitBox : MonoBehaviour
    {
        public delegate void OnHitEvent(HitBox myHitBox, HitBox otherHitBox);
        private event OnHitEvent _onHit = null;
        public event OnHitEvent OnHit
        {
            add
            {
                _onHit -= value;
                _onHit += value;
            }
            remove
            {
                _onHit -= value;
            }
        }

        /// <summary>
        /// 충돌 판정 타입
        /// </summary>
        public enum eHitBoxRole
        {
            /// <summary>
            /// 피격 판정용
            /// </summary>
            Hurt,
            /// <summary>
            /// 공격 판정용
            /// </summary>
            Attack,
        }

        #region Inspector

        /// <summary>
        /// 판정 타입
        /// </summary>
        public eHitBoxRole Role = eHitBoxRole.Hurt;

        [Header("Attack Sweep")]
        [Tooltip("Attack의 프레임 사이를 검사한다. Box, Sphere, Capsule 및 Convex MeshCollider를 지원한다.")]
        public bool isAttackSweepEnabled = false;

        [Tooltip("이동과 회전으로 움직이는 끝점의 검사 간격(월드 단위). 작을수록 검사가 촘촘해진다.")]
        [Min(0.001f)] public float sweepStepDistance = 0.05f;

        [Tooltip("프레임당 최대 분할 수. 한도를 넘는 이동은 검사 간격이 커진다.")]
        [Range(1, 256)] public int maxSweepSteps = 64;

        [Tooltip("중간 검사 영역의 유지 시간(게임 시간, 초). 0이면 한 프레임 표시한다.")]
        [Min(0f)] public float sweepDebugDuration = 0.5f;

        public Color sweepDebugColor = Color.yellow;
        public Color sweepDebugHitColor = Color.green;

        [Header("Debug")]
        public bool isHitDebugEnabled = false;
        public Color hitDebugColor = Color.yellow;

        [Tooltip("표시 유지 시간(게임 시간, 초). 0이면 한 프레임 표시한다.")]
        [Min(0f)] public float hitDebugDuration = 2f;

        [Tooltip("근사 감지 위치에 표시할 정육면체 표식의 전체 크기(월드 단위).")]
        [Min(0.01f)] public float hitDebugSize = 0.2f;

        public bool isHitDebugDepthTest = false;

        #endregion

        public Pawn Owner { get; private set; } = null;

        private Transform _transform;

        public Transform Transform
        {
            get
            {
                if (_transform == null)
                {
                    _transform = transform;
                }
                return _transform;
            }
        }

        /// <summary>
        /// 무시할 콜라이더 리스트
        /// </summary>
        private List<Collider> _ignores = new();

        /// <summary>
        /// 확인된 콜라이더 리스트
        /// </summary>
        private List<Collider> _colliders = new();

        protected Collider HitCollider { get; private set; }
        private Vector3 _previousPosition;
        private Quaternion _previousRotation;
        private Vector3 _previousScale;
        private bool _hasPreviousPose;
        private readonly PhysicsQueryHelper.PhysicsQueryDebug _shapeDebug = new();
        private Collider[] _sweepResults = new Collider[32];
        private readonly HashSet<Collider> _sweepContacts = new();
        private readonly HashSet<Collider> _frameSweepContacts = new();

        protected virtual void Awake()
        {
            Owner = GetComponentInParent<Pawn>();
            HitCollider = GetComponent<Collider>();
        }

        /// <summary>
        /// 무시할 콜라이더 추가
        /// </summary>
        /// <param name="colliders"></param>
        public void AddIgnoreCollider(params Collider[] colliders)
        {
            foreach (Collider collider in colliders)
            {
                _ignores.Add(collider);
            }
        }

        protected virtual void OnEnable()
        {
            ResetAttackSweep();
        }

        protected virtual void OnDisable()
        {
            _hasPreviousPose = false;
            _colliders.Clear();
            _sweepContacts.Clear();
            _frameSweepContacts.Clear();
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (HitCollider == null || !HitCollider.enabled || !isActiveAndEnabled)
            {
                return;
            }
            if (!CanHit(other, out _) || HasContact(other))
            {
                return;
            }
            ProcessHit(other, GetHitDebugPoint(other, HitCollider.bounds.center));
        }

        /// <summary>
        /// Trigger와 중간 검사의 제외 조건 및 접촉 중 중복 판정을 공유한다.
        /// </summary>
        protected void ProcessHit(Collider other, Vector3 point)
        {
            if (!CanHit(other, out HitBox hitBox) || HasContact(other))
            {
                return;
            }
            // 이벤트에서 판정을 꺼도 이력이 남지 않도록 호출 전에 등록한다.
            _colliders.Add(other);
            DrawHitDebug(point);
            _onHit?.Invoke(this, hitBox);
        }

        /// <summary>
        /// 같은 역할, 자신, 같은 소유자 및 명시된 제외 대상을 판정에서 제외한다.
        /// </summary>
        protected bool CanHit(Collider other, out HitBox hitBox)
        {
            hitBox = other != null ? other.GetComponent<HitBox>() : null;
            return hitBox != null && hitBox != this && hitBox.isActiveAndEnabled && hitBox.Role != Role
                && (Owner == null || hitBox.Owner != Owner) && !_ignores.Contains(other)
                && !Physics.GetIgnoreLayerCollision(gameObject.layer, other.gameObject.layer)
                && !Physics.GetIgnoreCollision(HitCollider, other);
        }

        /// <summary>
        /// Trigger와 프레임 사이 검사에서 확인한 접촉 이력으로 중복 이벤트를 막는다.
        /// </summary>
        protected virtual bool HasContact(Collider other)
        {
            return _colliders.Contains(other) || _sweepContacts.Contains(other);
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (!_sweepContacts.Contains(other))
            {
                _colliders.Remove(other);
            }
        }

        /// <summary>
        /// 장비 이전 시 판정 소유권과 이전 프레임의 스윕을 초기화한다.
        /// </summary>
        public void SetOwner(Pawn owner)
        {
            Owner = owner;
            ResetAttackSweep();
        }

        /// <summary>
        /// 공격 시작이나 순간이동 후 이전 이동 구간과 접촉 이력을 초기화한다.
        /// </summary>
        public void ResetAttackSweep()
        {
            _hasPreviousPose = false;
            _colliders.Clear();
            _sweepContacts.Clear();
            _frameSweepContacts.Clear();
            if (HitCollider != null && HitCollider.enabled)
            {
                SaveSweepPose();
            }
        }

        /// <summary>
        /// 애니메이션 갱신 후 이동과 회전을 분할하여 실제 콜라이더의 겹침을 확인한다.
        /// </summary>
        private void LateUpdate()
        {
            if (Role != eHitBoxRole.Attack || !isAttackSweepEnabled || HitCollider == null || !HitCollider.enabled)
            {
                _hasPreviousPose = false;
                _sweepContacts.Clear();
                DrawCurrentHitBoxDebug();
                return;
            }
            if (!_hasPreviousPose || _previousScale != Transform.lossyScale)
            {
                ResetAttackSweep();
            }
            if (!TryGetSweepBounds(out Vector3 localCenter, out Vector3 halfExtents))
            {
                _hasPreviousPose = false;
                return;
            }

            Vector3 position = Transform.position;
            Quaternion rotation = Transform.rotation;
            Vector3 scale = Transform.lossyScale;
            float reach = Vector3.Scale(localCenter, scale).magnitude + halfExtents.magnitude;
            float travel = Vector3.Distance(_previousPosition, position) + Quaternion.Angle(_previousRotation, rotation) * Mathf.Deg2Rad * reach;
            float spacing = float.IsNaN(sweepStepDistance) || float.IsInfinity(sweepStepDistance) ? 0.05f : Mathf.Max(0.001f, sweepStepDistance);
            int steps = Mathf.Clamp(Mathf.CeilToInt(travel / spacing), 1, Mathf.Clamp(maxSweepSteps, 1, 256));
            bool hasMoved = travel > 0.00001f;
            _frameSweepContacts.Clear();
            Physics.SyncTransforms();
            for (int step = 0; step <= steps; step++)
            {
                float t = (float)step / steps;
                Vector3 samplePosition = Vector3.Lerp(_previousPosition, position, t);
                Quaternion sampleRotation = Quaternion.Slerp(_previousRotation, rotation, t);
                Vector3 center = samplePosition + sampleRotation * Vector3.Scale(localCenter, scale);
                int count;
                do
                {
                    count = Physics.OverlapBoxNonAlloc(center, halfExtents, _sweepResults, sampleRotation, Physics.AllLayers, QueryTriggerInteraction.Collide);
                    if (count < _sweepResults.Length)
                    {
                        break;
                    }
                    System.Array.Resize(ref _sweepResults, _sweepResults.Length * 2);
                }
                while (true);

                bool hasHit = false;
                for (int i = 0; i < count; i++)
                {
                    Collider other = _sweepResults[i];
                    if (!CanHit(other, out _) || !Physics.ComputePenetration(HitCollider, samplePosition, sampleRotation, other, other.transform.position, other.transform.rotation, out _, out _))
                    {
                        continue;
                    }
                    hasHit = true;
                    _frameSweepContacts.Add(other);
                    ProcessHit(other, GetHitDebugPoint(other, center));
                    if (!isActiveAndEnabled || !HitCollider.enabled)
                    {
                        _hasPreviousPose = false;
                        return;
                    }
                }
                if (hasMoved || step == steps)
                {
                    DrawSweepDebug(center, halfExtents, sampleRotation, hasHit);
                }
            }
            // 이전 구간에서만 접촉한 대상도 다음 비접촉 프레임에서 해제한다.
            _colliders.RemoveAll(collider => !_frameSweepContacts.Contains(collider));
            _sweepContacts.Clear();
            _sweepContacts.UnionWith(_frameSweepContacts);
            SaveSweepPose();
        }

        /// <summary>
        /// 다음 프레임의 보간 시작 위치, 회전 및 스케일을 기록한다.
        /// </summary>
        private void SaveSweepPose()
        {
            _previousPosition = Transform.position;
            _previousRotation = Transform.rotation;
            _previousScale = Transform.lossyScale;
            _hasPreviousPose = true;
        }

        /// <summary>
        /// 콜라이더를 감싸는 회전된 박스를 후보 검색에 사용하고, 최종 겹침은 원래 형상으로 확인한다.
        /// </summary>
        private bool TryGetSweepBounds(out Vector3 localCenter, out Vector3 halfExtents)
        {
            Vector3 scale = Transform.lossyScale;
            Vector3 absoluteScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            localCenter = Vector3.zero;
            halfExtents = Vector3.zero;
            if (HitCollider is BoxCollider box)
            {
                localCenter = box.center;
                halfExtents = Vector3.Scale(box.size * 0.5f, absoluteScale);
            }
            else if (HitCollider is MeshCollider mesh && mesh.convex && mesh.sharedMesh != null)
            {
                localCenter = mesh.sharedMesh.bounds.center;
                halfExtents = Vector3.Scale(mesh.sharedMesh.bounds.extents, absoluteScale);
            }
            else if (HitCollider is SphereCollider sphere)
            {
                localCenter = sphere.center;
                halfExtents = Vector3.one * sphere.radius * Mathf.Max(absoluteScale.x, absoluteScale.y, absoluteScale.z);
            }
            else if (HitCollider is CapsuleCollider capsule)
            {
                localCenter = capsule.center;
                float radiusScale = capsule.direction == 0 ? Mathf.Max(absoluteScale.y, absoluteScale.z)
                    : capsule.direction == 1 ? Mathf.Max(absoluteScale.x, absoluteScale.z) : Mathf.Max(absoluteScale.x, absoluteScale.y);
                halfExtents = Vector3.one * capsule.radius * radiusScale;
                halfExtents[capsule.direction] = Mathf.Max(halfExtents[capsule.direction], capsule.height * absoluteScale[capsule.direction] * 0.5f);
            }
            else
            {
                return false;
            }
            halfExtents = Vector3.Max(halfExtents, Vector3.one * 0.0001f);
            return true;
        }

        /// <summary>
        /// 스윕을 수행하지 않을 때 추가 물리 조회 없이 현재 영역을 한 프레임 표시한다.
        /// </summary>
        private void DrawCurrentHitBoxDebug()
        {
            if (!(isHitDebugEnabled || GameConfig.IsCollisionDebugEnabled) || HitCollider == null || !HitCollider.enabled)
            {
                return;
            }
            _shapeDebug.drawMode = PhysicsQueryHelper.eDebugDrawMode.ForOneFrame;
            _shapeDebug.color = hitDebugColor;
            _shapeDebug.depthTest = isHitDebugDepthTest;
            Vector3 scale = Transform.lossyScale;
            Vector3 absoluteScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            if (HitCollider is SphereCollider sphere)
            {
                Vector3 center = Transform.position + Transform.rotation * Vector3.Scale(sphere.center, scale);
                float radius = sphere.radius * Mathf.Max(absoluteScale.x, absoluteScale.y, absoluteScale.z);
                PhysicsQueryHelper.DrawSphere(center, radius, _shapeDebug, false);
                return;
            }
            if (HitCollider is CapsuleCollider capsule)
            {
                Vector3 center = Transform.position + Transform.rotation * Vector3.Scale(capsule.center, scale);
                Vector3 localAxis = capsule.direction == 0 ? Vector3.right : capsule.direction == 1 ? Vector3.up : Vector3.forward;
                Vector3 axis = Transform.rotation * localAxis;
                float radiusScale = capsule.direction == 0 ? Mathf.Max(absoluteScale.y, absoluteScale.z)
                    : capsule.direction == 1 ? Mathf.Max(absoluteScale.x, absoluteScale.z) : Mathf.Max(absoluteScale.x, absoluteScale.y);
                float radius = capsule.radius * radiusScale;
                float halfSegment = Mathf.Max(0f, capsule.height * absoluteScale[capsule.direction] * 0.5f - radius);
                PhysicsQueryHelper.DrawCapsule(center - axis * halfSegment, center + axis * halfSegment, radius, _shapeDebug, false);
                return;
            }
            if (TryGetSweepBounds(out Vector3 localCenter, out Vector3 halfExtents))
            {
                Quaternion rotation = Transform.rotation;
                Vector3 center = Transform.position + rotation * Vector3.Scale(localCenter, Transform.lossyScale);
                DrawSweepDebug(center, halfExtents, rotation, false, false);
            }
            else
            {
                Bounds bounds = HitCollider.bounds;
                DrawSweepDebug(bounds.center, bounds.extents, Quaternion.identity, false, false);
            }
        }

        /// <summary>
        /// 중간 위치에서 후보 검색에 사용한 박스를 게임 화면에 남기고 실제 접촉 여부를 색으로 표시한다.
        /// </summary>
        private void DrawSweepDebug(Vector3 center, Vector3 halfExtents, Quaternion rotation, bool hasHit, bool isTrail = true)
        {
            if (!(isHitDebugEnabled || GameConfig.IsCollisionDebugEnabled))
            {
                return;
            }
            float duration = !isTrail || float.IsNaN(sweepDebugDuration) || float.IsInfinity(sweepDebugDuration) ? 0f : Mathf.Max(0f, sweepDebugDuration);
            Color color = isTrail ? (hasHit ? sweepDebugHitColor : sweepDebugColor) : hitDebugColor;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 start = center + rotation * GetSweepCorner(corner, halfExtents);
                for (int axis = 1; axis <= 4; axis <<= 1)
                {
                    if ((corner & axis) != 0)
                    {
                        continue;
                    }
                    Vector3 end = center + rotation * GetSweepCorner(corner | axis, halfExtents);
                    PhysicsQueryDebugRenderer.DrawLine(start, end, color, duration, isHitDebugDepthTest);
                }
            }
        }

        /// <summary>
        /// 배열을 생성하지 않고 비트의 부호로 박스 꼭짓점을 계산한다.
        /// </summary>
        private static Vector3 GetSweepCorner(int corner, Vector3 halfExtents)
        {
            return new Vector3((corner & 1) == 0 ? -halfExtents.x : halfExtents.x, (corner & 2) == 0 ? -halfExtents.y : halfExtents.y, (corner & 4) == 0 ? -halfExtents.z : halfExtents.z);
        }

        #region ## DEBUG ##

        /// <summary>
        /// 지원 형상은 표면 최근접점을 사용하고, 나머지는 월드 외접 박스의 근사점을 사용한다.
        /// </summary>
        private Vector3 GetHitDebugPoint(Collider other, Vector3 origin)
        {
            if (!(isHitDebugEnabled || GameConfig.IsCollisionDebugEnabled))
            {
                return origin;
            }
            if (other is BoxCollider || other is SphereCollider || other is CapsuleCollider || (other is MeshCollider mesh && mesh.convex))
            {
                return other.ClosestPoint(origin);
            }
            return other.bounds.ClosestPoint(origin);
        }

        /// <summary>
        /// 유효한 HitBox 감지 위치를 게임 화면에 정육면체 표식으로 표시한다.
        /// </summary>
        private void DrawHitDebug(Vector3 point)
        {
            if (!(isHitDebugEnabled || GameConfig.IsCollisionDebugEnabled))
            {
                return;
            }

            // Trigger에는 접촉점 정보가 없으므로 상대 콜라이더에 대한 최근접 위치로 근사한다.
            // 중간 검사는 해당 시점의 위치에서 계산한 근사점을 전달한다.
            float duration = hitDebugDuration;
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f)
            {
                duration = 0f;
            }

            float size = hitDebugSize;
            if (float.IsNaN(size) || float.IsInfinity(size) || size <= 0f)
            {
                size = 0.2f;
            }

            PhysicsQueryDebugRenderer.DrawCube(point, size, hitDebugColor, duration, isHitDebugDepthTest);
        }

        #endregion
    }
}
