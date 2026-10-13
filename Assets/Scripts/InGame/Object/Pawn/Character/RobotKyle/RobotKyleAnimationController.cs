#pragma warning disable IDE0051 // Unity Animation Event에서 호출한다.

using UnityEngine;

namespace Game
{
    public class RobotKyleAnimationController : CharacterAnimationController
    {
        private RobotKyle _roboyKlye;
        private int _upperBodyLayerIndex;

        public bool IsAttacking { get; private set; } = false;

        protected override void Awake()
        {
            base.Awake();
            _roboyKlye = GetComponentInParent<RobotKyle>();
            _upperBodyLayerIndex = boneAnimator.GetLayerIndex("UpperBody Layer");
        }

        /// <summary>
        /// 무기 애니메이션 세팅
        /// </summary>
        /// <param name="isMelee">근접 애니메이션 사용 여부</param>
        public void SetWeaponAnimation(bool isMelee)
        {
            // 부모 Pawn의 Awake가 먼저 호출되어도 초기 무기 설정을 적용한다.
            if (boneAnimator == null)
            {
                boneAnimator = GetComponent<Animator>();
                _upperBodyLayerIndex = boneAnimator.GetLayerIndex("UpperBody Layer");
            }
            if (isMelee)
            {
                boneAnimator.SetBool(AnimHash.IsMelee, true);
                boneAnimator.SetLayerWeight(_upperBodyLayerIndex, 0f);
            }
            else
            {
                boneAnimator.SetBool(AnimHash.IsMelee, false);
                boneAnimator.SetLayerWeight(_upperBodyLayerIndex, 1f);
            }
        }

        /// <summary>
        /// 공격을 시작하고 근접 공격일 때만 이동을 제한한다.
        /// </summary>
        public void StartAttack()
        {
            if (IsAttacking)
            {
                return;
            }

            IsAttacking = true;
            if (boneAnimator.GetBool(AnimHash.IsMelee))
            {
                _roboyKlye.Movement.isMoveEnable = false;

                if (_roboyKlye.Movement.IsGrounded)
                {
                    _roboyKlye.Movement.StopMove();
                }
            }

            boneAnimator.SetTrigger(AnimHash.Attack);
        }

        /// <summary>
        /// 공격 애니메이션 종료 이벤트
        /// </summary>
        private void OnAttackAnimationEnd()
        {
            IsAttacking = false;
            _roboyKlye.Movement.isMoveEnable = true;
        }

        /// <summary>
        /// 데미지 체크 시작
        /// </summary>
        private void OnStartDamageCheck()
        {
            _roboyKlye.EquippedWeapon?.StartAttack();
        }

        /// <summary>
        /// 데미지 체크 종료
        /// </summary>
        private void OnEndDamageCheck()
        {
            _roboyKlye.EquippedWeapon?.EndAttack();
        }

        /// <summary>
        /// 피격 받음
        /// </summary>
        public void OnHitted()
        {
            _roboyKlye.EquippedWeapon?.EndAttack();
            boneAnimator.SetTrigger(AnimHash.Hit);
        }
    }
}
