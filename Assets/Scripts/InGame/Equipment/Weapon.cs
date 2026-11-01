using UnityEngine;

namespace Game
{
    /// <summary>
    /// 무기의 공통 공격 간격과 장착 오프셋을 정의한다.
    /// </summary>
    public abstract class Weapon : Equipment
    {
        [Min(0)] public int damage = 20;
        [Min(0.05f)] public float attackInterval = 0.8f;
        [Min(0.05f)] public float attackDuration = 0.8f;
        [Min(0.1f)] public float range = 1.8f;
        public Vector3 equippedPosition;
        public Vector3 equippedEuler;
        public Vector3 sheathedPosition;
        public Vector3 sheathedEuler;
        private float _nextAttackAt;
        public virtual bool CanAttack => Owner != null && !Owner.IsDead && Owner.IsCombatEnabled && Owner.IsWeaponDrawn && Owner.EquippedWeapon == this && Time.time >= _nextAttackAt;
        public int Damage => Mathf.Max(0, damage + (Owner != null ? Owner.StatusInfo.baseStatus.damage : 0));

        /// <summary>
        /// 무기별 판정 전에 공통 공격 간격을 확보한다.
        /// </summary>
        public virtual bool TryAttack(Vector3 aimPoint)
        {
            if (!CanAttack)
            {
                return false;
            }
            _nextAttackAt = Time.time + Mathf.Max(0.05f, attackInterval);
            return true;
        }

        /// <summary>
        /// 애니메이션의 실제 공격 판정 구간을 시작한다.
        /// </summary>
        public virtual void StartAttack()
        {
        }

        /// <summary>
        /// 공격 판정과 무기별 대기 처리를 중단한다.
        /// </summary>
        public virtual void EndAttack()
        {
        }

        /// <summary>
        /// 라운드 재시작에 필요한 무기 상태를 초기화한다.
        /// </summary>
        public virtual void ResetWeapon()
        {
            EndAttack();
            _nextAttackAt = 0f;
        }
    }
}
