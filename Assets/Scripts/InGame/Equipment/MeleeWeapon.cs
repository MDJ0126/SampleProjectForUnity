using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 유효한 공격 구간에서 대상별 한 번의 근접 피해와 검기를 처리한다.
    /// </summary>
    public class MeleeWeapon : Weapon
    {
        public HitBox hitBox;
        public WeaponRibbon weaponRibbon;
        private readonly HashSet<Pawn> _hitPawns = new HashSet<Pawn>();

        protected virtual void OnEnable()
        {
            if (hitBox != null)
            {
                hitBox.OnHit += OnHit;
                hitBox.enabled = false;
            }
        }

        protected virtual void OnDisable()
        {
            EndAttack();
            _hitPawns.Clear();
            if (hitBox != null)
            {
                hitBox.OnHit -= OnHit;
            }
            if (weaponRibbon != null)
            {
                weaponRibbon.ClearTrail();
            }
        }

        /// <summary>
        /// 소유자가 바뀌면 피격 판정의 소유권도 함께 갱신한다.
        /// </summary>
        public override void SetOwner(Pawn owner)
        {
            EndAttack();
            base.SetOwner(owner);
            if (hitBox != null)
            {
                hitBox.SetOwner(owner);
            }
        }

        /// <summary>
        /// 현재 공격 중인 선택 무기에만 판정을 열고 검기 미리보기도 지원한다.
        /// </summary>
        public override void StartAttack()
        {
            if (Owner != null && (!Owner.IsAttacking || Owner.IsDead || !Owner.IsWeaponDrawn || Owner.EquippedWeapon != this))
            {
                return;
            }
            _hitPawns.Clear();
            if (hitBox != null)
            {
                hitBox.enabled = Owner != null;
            }
            if (weaponRibbon != null)
            {
                weaponRibbon.BeginTrail();
            }
        }

        /// <summary>
        /// 공격 취소와 애니메이션 종료에서 동일한 판정 해제를 사용한다.
        /// </summary>
        public override void EndAttack()
        {
            if (hitBox != null)
            {
                hitBox.enabled = false;
            }
            if (weaponRibbon != null)
            {
                weaponRibbon.EndTrail();
            }
        }

        /// <summary>
        /// 자기 팀·중복 피격·취소된 공격을 제외하고 무기 피해를 적용한다.
        /// </summary>
        private void OnHit(HitBox own, HitBox other)
        {
            Pawn target = other != null ? other.Owner : null;
            if (Owner != null && Owner.IsAttacking && other != null && other.Role == HitBox.eHitBoxRole.Hurt && Owner.CanDamage(target) && _hitPawns.Add(target))
            {
                target.Damaged(Owner, new DamageInfo { damage = Damage });
            }
        }
    }
}
