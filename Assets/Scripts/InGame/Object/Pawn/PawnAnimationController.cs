using UnityEngine;

namespace Game
{
    /// <summary>
    /// Pawn 상태와 애니메이션 재생 사이의 최소 연결을 제공한다.
    /// </summary>
    public abstract class PawnAnimationController : MonoBehaviour
    {
        protected Animator boneAnimator;

        protected virtual void Awake()
        {
            boneAnimator = GetComponent<Animator>();
        }

        /// <summary>
        /// 파생 애니메이션 컨트롤러에서 공격 동작을 재생한다.
        /// </summary>
        public virtual void PlayAttack()
        {
        }

        /// <summary>
        /// 파생 애니메이션 컨트롤러에서 생존 중 피격 동작을 재생한다.
        /// </summary>
        public virtual void PlayHit()
        {
        }

        /// <summary>
        /// 파생 애니메이션 컨트롤러에서 사망 동작을 재생한다.
        /// </summary>
        public virtual void PlayDeath()
        {
        }

        /// <summary>
        /// 구현한 뽑기 동작의 길이를 반환하며 없는 모델은 즉시 소켓을 전환한다.
        /// </summary>
        public virtual float PlayWeaponDraw(bool drawn)
        {
            return 0f;
        }

        /// <summary>
        /// 교체와 재시작에서 이전 공격의 애니메이션 이벤트를 중단한다.
        /// </summary>
        public virtual void ResetState()
        {
            if (boneAnimator == null)
            {
                boneAnimator = GetComponent<Animator>();
            }
            if (boneAnimator != null && boneAnimator.runtimeAnimatorController != null)
            {
                boneAnimator.Rebind();
                boneAnimator.Update(0f);
            }
        }
    }
}
