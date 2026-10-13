using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Game
{
    /// <summary>
    /// 폰을 상속 받으며, 폰의 기능에 더해 이동 기능이 추가된 액터
    /// </summary>
    public abstract class Character : Pawn
    {
        #region Inspector

        public AimTarget aimTarget;
        public MultiAimConstraint headAim;

        #endregion

        public override string Name => nameof(Character);
        public CharacterAnimationController CharacterAnimationController { get; private set; }
        public Vector3 AimTargetDefault { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            AimTargetDefault = aimTarget.target.localPosition;
            CharacterAnimationController = GetComponent<CharacterAnimationController>();
        }

        public void ResetAimTargetPosition()
        {
            aimTarget.target.localPosition = AimTargetDefault;
        }

        /// <summary>
        /// 빙의한 컨트롤러가 캐릭터의 에임 타겟을 조작하도록 연결
        /// </summary>
        public override void Possess(PawnController pawnController)
        {
            base.Possess(pawnController);
            pawnController.aimTarget = aimTarget;
        }

        /// <summary>
        /// 빙의 해제 시 변경된 에임 타겟 위치 복원
        /// </summary>
        public override void Unpossess(PawnController pawnController)
        {
            base.Unpossess(pawnController);
            ResetAimTargetPosition();
        }
    }
}
