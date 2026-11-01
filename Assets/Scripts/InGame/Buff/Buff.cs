using System;

namespace Game
{
    /// <summary>
    /// 유효한 경과 시간을 누적하고 종료를 한 번만 알린다.
    /// </summary>
    public abstract class Buff
    {
        public event Action<Buff> OnEndBuff;
        protected Pawn owner;
        protected float time;
        protected float duration;
        public float RemainingSeconds => Math.Max(0f, duration - time);
        public bool IsExpired => time >= duration;

        /// <summary>
        /// 지속 효과를 적용할 Pawn을 연결한다.
        /// </summary>
        public void SetOwner(Pawn pawn)
        {
            owner = pawn;
        }

        /// <summary>
        /// 지속 시간이 끝나면 목록 제거 이벤트를 한 번만 호출한다.
        /// </summary>
        public virtual void UpdateTick(float deltaTime)
        {
            time = Math.Min(duration, time + Math.Max(0f, deltaTime));
            if (IsExpired)
            {
                Action<Buff> ended = OnEndBuff;
                OnEndBuff = null;
                ended?.Invoke(this);
            }
        }
    }
}
