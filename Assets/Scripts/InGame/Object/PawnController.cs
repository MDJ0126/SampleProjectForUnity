using System;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 카메라와 입력 장치에 의존하지 않는 Pawn 빙의 소유권을 관리한다.
    /// </summary>
    public abstract class PawnController : MonoBehaviour
    {
        public event Action<Pawn> OnPossessed;
        public event Action<Pawn> OnUnpossessed;
        [ReadOnly, SerializeField] protected Pawn possessTarget;
        [ReadOnly] public AimTarget aimTarget;
        public float aimDistance = 100f;
        public LayerMask aimMask = ~0;
        private Pawn _lastPawn;
        public Pawn PossessedPawn => possessTarget;
        public bool IsPossessed => possessTarget != null;

        protected virtual void OnDestroy()
        {
            Unpossess();
            OnPossessed = null;
            OnUnpossessed = null;
        }

        /// <summary>
        /// 다른 컨트롤러가 소유하지 않은 살아 있는 Pawn에 빙의한다.
        /// </summary>
        public virtual void Possess(Pawn pawn = null)
        {
            pawn = pawn != null ? pawn : _lastPawn;
            if (pawn == null || pawn.IsDead || pawn == possessTarget || (pawn.Controller != null && pawn.Controller != this))
            {
                return;
            }
            Unpossess();
            possessTarget = pawn;
            _lastPawn = pawn;
            pawn.Possess(this);
            OnPossessionChanged();
            OnPossessed?.Invoke(pawn);
        }

        /// <summary>
        /// 입력과 조준 상태를 해제하고 Pawn 소유권을 반환한다.
        /// </summary>
        public virtual void Unpossess()
        {
            Pawn previous = possessTarget;
            if (previous == null)
            {
                return;
            }
            previous.CancelAttack();
            previous.Movement?.StopMove();
            previous.Unpossess(this);
            possessTarget = null;
            aimTarget = null;
            OnPossessionChanged();
            OnUnpossessed?.Invoke(previous);
        }

        /// <summary>
        /// 플레이어 입력이나 AI 탐색처럼 컨트롤러별 연결만 갱신한다.
        /// </summary>
        protected virtual void OnPossessionChanged()
        {
        }
    }
}
