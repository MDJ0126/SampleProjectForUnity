using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 소유권·장비·공격·생명 상태를 플레이어와 AI가 함께 사용한다.
    /// </summary>
    public abstract class Pawn : Actor
    {
        public event Action<Pawn, Pawn> OnDamaged;
        public event Action<Pawn> OnDied;
        public WidgetAnchor PawnInfoAnchor;
        public WidgetAnchor balloonAnchor;
        public List<Equipment> equipmentList = new List<Equipment>();
        public int teamId;
        public Transform weaponSocket;
        public Transform holsterSocket;
        public EquipmentLoadout loadout;
        public int defaultWeaponIndex;
        public string displayName;
        private static readonly List<Pawn> _active = new List<Pawn>();
        private readonly List<FollowHUD> _followHUDs = new List<FollowHUD>();
        private Weapon _attackingWeapon;
        private float _attackEndsAt;
        private float _drawEndsAt;
        private float _drawSwitchAt;
        private bool _pendingDraw;
        private bool _deathNotified;
        private Rigidbody _body;
        private Collider _bodyCollider;
        private bool _wasKinematic;
        public static IReadOnlyList<Pawn> ActivePawns => _active;
        public virtual string Name => string.IsNullOrEmpty(displayName) ? GetType().Name : displayName;
        public StatusInfo StatusInfo { get; } = new StatusInfo();
        public SkillManager SkillManager { get; } = new SkillManager();
        public BuffManager BuffManager { get; } = new BuffManager();
        public Movement Movement { get; private set; }
        public PawnAnimationController PawnAnimationController { get; private set; }
        public PawnController Controller { get; private set; }
        public Weapon EquippedWeapon { get; private set; }
        public bool IsWeaponDrawn { get; private set; } = true;
        public bool IsAttacking { get; private set; }
        public bool IsChangingWeapon => _drawEndsAt > 0f;
        public bool IsDead => StatusInfo.IsDead;
        public bool IsCombatEnabled { get; private set; } = true;
        public bool CanMove => IsCombatEnabled && !IsDead && !IsChangingWeapon && !(IsAttacking && _attackingWeapon is MeleeWeapon);
        public Vector3 AimPoint => _bodyCollider != null ? _bodyCollider.bounds.center : transform.position + Vector3.up;

        protected override void Awake()
        {
            base.Awake();
            Movement = GetComponent<Movement>();
            PawnAnimationController = GetComponentInChildren<PawnAnimationController>();
            _body = GetComponent<Rigidbody>();
            _bodyCollider = GetComponentInChildren<CapsuleCollider>();
            _wasKinematic = _body != null && _body.isKinematic;
            equipmentList ??= new List<Equipment>();
            HashSet<Equipment> seen = new HashSet<Equipment>();
            for (int i = 0; i < equipmentList.Count; i++)
            {
                Equipment item = equipmentList[i];
                if (item == null || !seen.Add(item) || (item.Owner != null && item.Owner != this))
                {
                    equipmentList.RemoveAt(i--);
                    continue;
                }
                item.SetOwner(this);
            }
            Initalize();
            StatusInfo.Initialize();
            SelectWeapon(loadout != null ? loadout.SelectedIndex : defaultWeaponIndex);
        }

        protected override void Start()
        {
            base.Start();
            SkillManager.SetOwner(this);
            BuffManager.SetOwner(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!_active.Contains(this))
            {
                _active.Add(this);
            }
            StatusInfo.OnChangedHp += OnHealthChanged;
            HUDManager hud = HUDManager.Instance;
            if (hud != null && PawnInfoAnchor != null)
            {
                _followHUDs.Add(hud.AttachFollowPawnInfo(this));
            }
        }

        protected override void OnDisable()
        {
            CancelAttack();
            Controller?.Unpossess();
            _active.Remove(this);
            StatusInfo.OnChangedHp -= OnHealthChanged;
            if (HUDManager.IsLive)
            {
                HUDManager.Instance.DetachFollowHUD(_followHUDs);
            }
            _followHUDs.Clear();
            base.OnDisable();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (IsChangingWeapon)
            {
                if (_drawSwitchAt > 0f && Time.time >= _drawSwitchAt)
                {
                    _drawSwitchAt = 0f;
                    ApplyWeaponDrawn(_pendingDraw, false);
                }
                if (Time.time >= _drawEndsAt)
                {
                    _drawEndsAt = 0f;
                }
            }
            if (IsAttacking && Time.time >= _attackEndsAt)
            {
                CancelAttack(false);
            }
            if (IsCombatEnabled && !IsDead)
            {
                SkillManager.UpdateTick(Time.deltaTime);
                BuffManager.UpdateTick(Time.deltaTime);
            }
        }

        /// <summary>
        /// 새 Play에서 이전 씬의 활성 Pawn 목록을 이어받지 않게 한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            _active.Clear();
        }

        /// <summary>
        /// 파생 Pawn의 기본 스테이터스를 설정한다.
        /// </summary>
        protected abstract void Initalize();

        /// <summary>
        /// 타 소유자의 장비는 거부하고 보유 장비의 선택도 처리한다.
        /// </summary>
        public bool Equip(Equipment equipment)
        {
            if (equipment == null || IsDead || (equipment.Owner != null && equipment.Owner != this))
            {
                return false;
            }
            if (!equipmentList.Contains(equipment))
            {
                equipmentList.Add(equipment);
            }
            equipment.SetOwner(this);
            if (equipment is Weapon weapon)
            {
                CancelAttack();
                if (EquippedWeapon != null && EquippedWeapon != weapon)
                {
                    EquippedWeapon.gameObject.SetActive(false);
                }
                EquippedWeapon = weapon;
                IsWeaponDrawn = true;
                foreach (Equipment item in equipmentList)
                {
                    if (item is Weapon other)
                    {
                        other.gameObject.SetActive(other == weapon);
                    }
                }
                MountWeapon();
                PawnAnimationController?.ResetState();
                OnEquippedWeaponChanged();
            }
            return true;
        }

        /// <summary>
        /// 보유 무기의 인덱스로 선택하고 공유 장비 선택 데이터에 반영한다.
        /// </summary>
        public bool SelectWeapon(int index)
        {
            int current = 0;
            foreach (Equipment item in equipmentList)
            {
                if (item is Weapon weapon && current++ == index)
                {
                    bool selected = Equip(weapon);
                    if (selected && loadout != null)
                    {
                        loadout.Select(index);
                    }
                    return selected;
                }
            }
            return false;
        }

        /// <summary>
        /// 보유 무기 목록을 순환하되 현재 공격 판정은 먼저 취소한다.
        /// </summary>
        public bool SelectNextWeapon()
        {
            int index = 0;
            int selected = -1;
            foreach (Equipment item in equipmentList)
            {
                if (item is Weapon weapon)
                {
                    if (weapon == EquippedWeapon)
                    {
                        selected = index;
                    }
                    index++;
                }
            }
            return index > 0 && SelectWeapon((selected + 1) % index);
        }

        /// <summary>
        /// 장비를 목록과 소유권에서 해제하고 선택 무기의 연결을 정리한다.
        /// </summary>
        public bool Unequip(Equipment equipment)
        {
            if (equipment == null || !equipmentList.Remove(equipment))
            {
                return false;
            }
            if (equipment == EquippedWeapon)
            {
                CancelAttack();
                EquippedWeapon = null;
                OnEquippedWeaponChanged();
            }
            equipment.SetOwner(null);
            equipment.gameObject.SetActive(false);
            return true;
        }

        /// <summary>
        /// 같은 무기를 손과 수납 소켓 사이로 옮기며 공격을 취소한다.
        /// </summary>
        public void SetWeaponDrawn(bool drawn, bool animate = false)
        {
            if (IsDead || EquippedWeapon == null || IsWeaponDrawn == drawn || IsChangingWeapon)
            {
                return;
            }
            CancelAttack();
            float duration = animate && PawnAnimationController != null ? PawnAnimationController.PlayWeaponDraw(drawn) : 0f;
            if (duration > 0f)
            {
                _pendingDraw = drawn;
                _drawSwitchAt = Time.time + duration * 0.45f;
                _drawEndsAt = Time.time + duration;
                return;
            }
            ApplyWeaponDrawn(drawn, true);
        }

        /// <summary>
        /// 손을 뻗는 구간에서 소켓을 교체하며 재생 중인 뽑기 동작을 유지한다.
        /// </summary>
        private void ApplyWeaponDrawn(bool drawn, bool resetAnimation)
        {
            IsWeaponDrawn = drawn;
            MountWeapon();
            if (resetAnimation)
            {
                PawnAnimationController?.ResetState();
            }
            OnEquippedWeaponChanged();
        }

        /// <summary>
        /// 무기별 로컬 오프셋을 적용하여 장착 표시를 공통 처리한다.
        /// </summary>
        private void MountWeapon()
        {
            Transform socket = IsWeaponDrawn ? weaponSocket : holsterSocket;
            if (socket != null)
            {
                EquippedWeapon.transform.SetParent(socket, false);
                EquippedWeapon.transform.localPosition = IsWeaponDrawn ? EquippedWeapon.equippedPosition : EquippedWeapon.sheathedPosition;
                EquippedWeapon.transform.localRotation = Quaternion.Euler(IsWeaponDrawn ? EquippedWeapon.equippedEuler : EquippedWeapon.sheathedEuler);
            }
            EquippedWeapon.gameObject.SetActive(true);
        }

        /// <summary>
        /// 입력과 AI가 같은 공격·수납·쿨다운·사망 조건을 사용한다.
        /// </summary>
        public bool TryAttack(Vector3 aimPoint)
        {
            Weapon weapon = EquippedWeapon;
            if (!IsCombatEnabled || IsDead || !IsWeaponDrawn || IsAttacking || IsChangingWeapon || weapon == null || !weapon.CanAttack)
            {
                return false;
            }
            IsAttacking = true;
            _attackingWeapon = weapon;
            _attackEndsAt = Time.time + weapon.attackDuration;
            if (!weapon.TryAttack(aimPoint))
            {
                CancelAttack();
                return false;
            }
            if (!IsDead)
            {
                PawnAnimationController?.PlayAttack();
                if (weapon is MeleeWeapon && PawnAnimationController == null)
                {
                    weapon.StartAttack();
                }
            }
            return true;
        }

        /// <summary>
        /// 공격과 장전의 남은 판정을 즉시 중단한다.
        /// </summary>
        public void CancelAttack(bool cancelReload = true)
        {
            if (cancelReload)
            {
                _drawEndsAt = 0f;
                _drawSwitchAt = 0f;
            }
            if (_attackingWeapon is MeleeWeapon || cancelReload)
            {
                _attackingWeapon?.EndAttack();
            }
            if (cancelReload && EquippedWeapon != _attackingWeapon)
            {
                EquippedWeapon?.EndAttack();
            }
            _attackingWeapon = null;
            IsAttacking = false;
        }

        /// <summary>
        /// 메뉴와 전환의 입력 차단을 이동과 공격에 함께 적용한다.
        /// </summary>
        public void SetCombatEnabled(bool enabled)
        {
            if (IsCombatEnabled == enabled)
            {
                return;
            }
            IsCombatEnabled = enabled;
            if (!enabled)
            {
                CancelAttack();
                Movement?.StopMove();
            }
        }

        /// <summary>
        /// 자신과 같은 팀 및 이미 죽은 대상에는 공격 피해를 주지 않는다.
        /// </summary>
        public bool CanDamage(Pawn target)
        {
            return isActiveAndEnabled && IsCombatEnabled && !IsDead && target != null && target != this && target.isActiveAndEnabled && !target.IsDead && target.IsCombatEnabled && teamId >= 0 && target.teamId >= 0 && target.teamId != teamId;
        }

        /// <summary>
        /// 유효 피해와 방어력을 적용하고 사망 후 중복 피격을 거부한다.
        /// </summary>
        public bool Damaged(Pawn attacker, DamageInfo damageInfo)
        {
            if (IsDead || !IsCombatEnabled || damageInfo.damage <= 0 || (attacker != null && !attacker.CanDamage(this)))
            {
                return false;
            }
            int damage = Math.Max(0, damageInfo.damage - Math.Max(0, StatusInfo.baseStatus.defence));
            if (damage == 0)
            {
                return false;
            }
            CancelAttack();
            StatusInfo.Damaged(damage);
            if (!IsDead)
            {
                PawnAnimationController?.PlayHit();
            }
            OnDamaged?.Invoke(attacker, this);
            return true;
        }

        /// <summary>
        /// 체력의 생사 전환을 감지하여 사망 처리를 한 번만 수행한다.
        /// </summary>
        private void OnHealthChanged(StatusInfo status)
        {
            if (!status.IsDead || _deathNotified)
            {
                return;
            }
            _deathNotified = true;
            CancelAttack();
            Movement?.StopMove();
            if (Movement != null)
            {
                Movement.enabled = false;
            }
            if (_body != null)
            {
                _body.isKinematic = true;
            }
            if (_bodyCollider != null)
            {
                _bodyCollider.enabled = false;
            }
            Controller?.Unpossess();
            PawnAnimationController?.PlayDeath();
            OnDied?.Invoke(this);
        }

        /// <summary>
        /// 라운드 재시작에서 체력·공격·물리·탄약·애니메이션을 복원한다.
        /// </summary>
        public void ResetCombat()
        {
            CancelAttack();
            _deathNotified = false;
            if (_body != null)
            {
                _body.isKinematic = _wasKinematic;
            }
            if (_bodyCollider != null)
            {
                _bodyCollider.enabled = true;
            }
            if (Movement != null)
            {
                Movement.enabled = true;
                Movement.StopMove();
            }
            StatusInfo.Initialize();
            foreach (Equipment item in equipmentList)
            {
                if (item is Weapon weapon)
                {
                    weapon.ResetWeapon();
                }
            }
            IsWeaponDrawn = true;
            if (EquippedWeapon != null)
            {
                MountWeapon();
            }
            PawnAnimationController?.ResetState();
            OnEquippedWeaponChanged();
        }

        /// <summary>
        /// 무기 변경 후 파생 Pawn의 애니메이션과 그립 연결을 갱신한다.
        /// </summary>
        protected virtual void OnEquippedWeaponChanged()
        {
        }

        /// <summary>
        /// 컨트롤러가 소유하는 Pawn 연결을 저장한다.
        /// </summary>
        public virtual void Possess(PawnController controller)
        {
            Controller = controller;
        }

        /// <summary>
        /// 현재 소유자만 Pawn 연결을 해제한다.
        /// </summary>
        public virtual void Unpossess(PawnController controller)
        {
            if (Controller == controller)
            {
                Controller = null;
            }
        }
    }
}
