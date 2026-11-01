using UnityEngine;

namespace Game
{
    /// <summary>
    /// 총구 차폐·탄약·장전·히트스캔 피해와 짧은 궤적을 처리한다.
    /// </summary>
    public class Gun : Weapon
    {
        [SerializeField] private Transform _rightGripTarget;
        [SerializeField] private Transform _leftGripTarget;
        [SerializeField] private Transform _muzzle;
        public int magazineSize = 30;
        public int reserveAmmo = 90;
        public float reloadDuration = 1.2f;
        public bool isAutomatic = true;
        public LayerMask hitMask = ~0;
        public LineRenderer tracer;
        private float _reloadEndsAt;
        private float _tracerEndsAt;
        public Transform RightGripTarget => _rightGripTarget;
        public Transform LeftGripTarget => _leftGripTarget;
        public Transform Muzzle => _muzzle;
        public int Ammo { get; private set; }
        public int Reserve { get; private set; }
        public bool IsReloading => _reloadEndsAt > 0f;
        public override bool CanAttack => base.CanAttack && Ammo > 0 && !IsReloading;
        public Vector3 LastHitPoint { get; private set; }
        public Pawn LastHitPawn { get; private set; }
        public int ShotsFired { get; private set; }

        private void Awake()
        {
            ResetWeapon();
        }

        private void Update()
        {
            if (IsReloading && Time.time >= _reloadEndsAt)
            {
                int amount = Mathf.Min(Mathf.Max(1, magazineSize) - Ammo, Reserve);
                Ammo += amount;
                Reserve -= amount;
                _reloadEndsAt = 0f;
            }
            if (tracer != null && tracer.enabled && Time.time >= _tracerEndsAt)
            {
                tracer.enabled = false;
            }
        }

        private void OnDisable()
        {
            EndAttack();
        }

        private void Reset()
        {
            range = 80f;
            attackInterval = 0.25f;
            attackDuration = 0.15f;
            damage = 10;
        }

        /// <summary>
        /// 몸에서 총구까지의 차폐와 총구 발사선을 순서대로 판정한다.
        /// </summary>
        public override bool TryAttack(Vector3 aimPoint)
        {
            if (!base.TryAttack(aimPoint))
            {
                return false;
            }
            Ammo--;
            ShotsFired++;
            Vector3 origin = _muzzle != null ? _muzzle.position : transform.position;
            Vector3 direction = aimPoint - origin;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            Vector3 barrel = origin - Owner.AimPoint;
            RaycastHit hit = default;
            bool blocked = barrel.sqrMagnitude > 0.0001f && GameUtils.TryRaycast(new Ray(Owner.AimPoint, barrel.normalized), barrel.magnitude, hitMask, Owner, out hit);
            if (!blocked)
            {
                blocked = GameUtils.TryRaycast(new Ray(origin, direction), range, hitMask, Owner, out hit);
            }
            LastHitPoint = blocked ? hit.point : origin + direction * range;
            LastHitPawn = blocked && hit.collider != null ? hit.collider.GetComponentInParent<Pawn>() : null;
            if (Owner.CanDamage(LastHitPawn))
            {
                LastHitPawn.Damaged(Owner, new DamageInfo { damage = Damage });
            }
            if (tracer != null)
            {
                tracer.useWorldSpace = true;
                tracer.positionCount = 2;
                tracer.SetPosition(0, origin);
                tracer.SetPosition(1, LastHitPoint);
                tracer.enabled = true;
                _tracerEndsAt = Time.time + 0.06f;
            }
            return true;
        }

        /// <summary>
        /// 살아 있는 소유자의 선택 무기만 장전하며 잔탄을 중복 생성하지 않는다.
        /// </summary>
        public bool Reload()
        {
            if (Owner == null || Owner.IsDead || Owner.IsAttacking || !Owner.IsCombatEnabled || !Owner.IsWeaponDrawn || Owner.EquippedWeapon != this || IsReloading || Ammo >= Mathf.Max(1, magazineSize) || Reserve <= 0)
            {
                return false;
            }
            _reloadEndsAt = Time.time + Mathf.Max(0.05f, reloadDuration);
            return true;
        }

        /// <summary>
        /// 무기 교체·피격·수납 시 진행 중 장전과 궤적을 취소한다.
        /// </summary>
        public override void EndAttack()
        {
            _reloadEndsAt = 0f;
            if (tracer != null)
            {
                tracer.enabled = false;
            }
        }

        /// <summary>
        /// 새 라운드에서 탄창과 예비탄을 초기 설정으로 복원한다.
        /// </summary>
        public override void ResetWeapon()
        {
            base.ResetWeapon();
            Ammo = Mathf.Max(1, magazineSize);
            Reserve = Mathf.Max(0, reserveAmmo);
            ShotsFired = 0;
            LastHitPawn = null;
        }
    }
}
