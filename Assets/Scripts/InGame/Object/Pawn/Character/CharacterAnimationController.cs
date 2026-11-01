using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(Animator))]
    public abstract partial class CharacterAnimationController : PawnAnimationController
    {
        private Character _character;
        private Movement _movement;
        private LayerMask _groundLayer;

        /// <summary>
        /// 애니메이션 갱신에 필요한 소유 캐릭터, 이동, Animator 및 지면 레이어 캐싱
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            _character = GetComponentInParent<Character>();
            _movement = _character.GetComponent<Movement>();
            _groundLayer = LayerMask.GetMask("Ground");
        }

        /// <summary>
        /// 이동 상태를 Animator 파라미터에 매 프레임 반영
        /// </summary>
        private void Update()
        {
            if (_character.IsDead)
            {
                return;
            }
            boneAnimator.SetFloat(AnimHash.Speed, _character.Movement.NormalizedHorizontalVelocity);
            boneAnimator.SetBool(AnimHash.IsSprint, _character.Movement.IsSprint);
            boneAnimator.SetBool(AnimHash.IsJumping, _movement.IsJumping);
            boneAnimator.SetBool(AnimHash.IsFalling, _movement.IsFalling);
            boneAnimator.SetBool(AnimHash.IsGrounded, _movement.IsGrounded);
            UpdateMoveAnimation();
        }

        /// <summary>
        /// 월드 이동 속도를 캐릭터 로컬 방향으로 바꾸어 블렌드 트리에 전달
        /// </summary>
        private void UpdateMoveAnimation()
        {
            Vector3 velocity = _character.Movement.HorizontalVelocity;

            Vector3 localDir = Vector3.zero;

            // 실제 이동 중일 때만 전후·좌우 방향값을 계산한다.
            if (_character.Movement.IsCanMove && velocity.sqrMagnitude > 0.001f)
            {
                localDir = transform.InverseTransformDirection(velocity.normalized);
            }

            const float DAMP_TIME = 0.1f;

            // 급격한 방향 전환에도 블렌드 값이 튀지 않도록 댐핑한다.
            boneAnimator.SetFloat(AnimHash.PosX, localDir.x, DAMP_TIME, Time.deltaTime);
            boneAnimator.SetFloat(AnimHash.PosY, localDir.z, DAMP_TIME, Time.deltaTime);
        }
    }
}
