#pragma warning disable IDE0051
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 공통 Pawn 전투 상태를 Robot Kyle의 애니메이션 이벤트에 연결한다.
    /// </summary>
    public class RobotKyleAnimationController : CharacterAnimationController
    {
        private RobotKyle _owner;
        private int _upperBodyLayer;
        public float drawDuration = 1f;
        public float sheathDuration = 1f;
        public bool IsAttacking => _owner != null && _owner.IsAttacking;

        protected override void Awake()
        {
            base.Awake();
            _owner = GetComponentInParent<RobotKyle>();
            _upperBodyLayer = boneAnimator.GetLayerIndex("UpperBody Layer");
        }

        /// <summary>
        /// 근접과 총기 자세를 선택하고 초기 Awake 순서도 허용한다.
        /// </summary>
        public void SetWeaponAnimation(bool isMelee)
        {
            if (boneAnimator == null)
            {
                boneAnimator = GetComponent<Animator>();
                _upperBodyLayer = boneAnimator.GetLayerIndex("UpperBody Layer");
            }
            boneAnimator.SetBool(AnimHash.IsMelee, isMelee);
            if (_upperBodyLayer >= 0)
            {
                boneAnimator.SetLayerWeight(_upperBodyLayer, isMelee ? 0f : 1f);
            }
        }

        /// <summary>
        /// 기존 호출도 공통 Pawn 공격 조건을 통과하도록 연결한다.
        /// </summary>
        public void StartAttack()
        {
            Vector3 point = _owner.aimTarget != null && _owner.aimTarget.target != null ? _owner.aimTarget.target.position : _owner.AimPoint + _owner.transform.forward * 80f;
            _owner.TryAttack(point);
        }

        /// <summary>
        /// 이미 승인된 공격의 시각 동작만 시작한다.
        /// </summary>
        public override void PlayAttack()
        {
            boneAnimator.SetTrigger(AnimHash.Attack);
        }

        /// <summary>
        /// 생존 중 피격 동작만 시작한다.
        /// </summary>
        public override void PlayHit()
        {
            boneAnimator.ResetTrigger(AnimHash.Attack);
            boneAnimator.SetTrigger(AnimHash.Hit);
        }

        /// <summary>
        /// 사망 시 상체 조준을 해제하고 사망 상태를 재생한다.
        /// </summary>
        public override void PlayDeath()
        {
            if (_owner.gunRigBuilder != null)
            {
                _owner.gunRigBuilder.enabled = false;
            }
            if (_upperBodyLayer >= 0)
            {
                boneAnimator.SetLayerWeight(_upperBodyLayer, 0f);
            }
            boneAnimator.SetBool(AnimHash.IsDead, true);
        }

        /// <summary>
        /// 검의 기존 등 수납·뽑기 클립을 재생하고 실제 클립 길이를 사용한다.
        /// </summary>
        public override float PlayWeaponDraw(bool drawn)
        {
            if (!(_owner.EquippedWeapon is MeleeWeapon))
            {
                return 0f;
            }
            boneAnimator.ResetTrigger(AnimHash.Attack);
            boneAnimator.ResetTrigger(AnimHash.Hit);
            boneAnimator.CrossFadeInFixedTime(drawn ? "Draw" : "Sheathe", 0.08f, 0);
            return drawn ? drawDuration : sheathDuration;
        }

        /// <summary>
        /// 실제 공격이 끝나면 타이머와 동일한 판정 해제를 사용한다.
        /// </summary>
        private void OnAttackAnimationEnd()
        {
            _owner.CancelAttack(false);
        }

        /// <summary>
        /// 근접 판정만 열어 총기의 중복 애니메이션 발사를 방지한다.
        /// </summary>
        private void OnStartDamageCheck()
        {
            if (_owner.IsAttacking && _owner.EquippedWeapon is MeleeWeapon melee)
            {
                melee.StartAttack();
            }
        }

        /// <summary>
        /// 근접 애니메이션의 피해 구간만 닫는다.
        /// </summary>
        private void OnEndDamageCheck()
        {
            if (_owner.EquippedWeapon is MeleeWeapon melee)
            {
                melee.EndAttack();
            }
        }
    }
}
