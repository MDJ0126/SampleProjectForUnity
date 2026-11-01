using System;

namespace Game
{
    /// <summary>
    /// 지속 시간 동안 초당 10의 체력을 프레임 간격과 무관하게 회복한다.
    /// </summary>
    public class HealBuff : Buff
    {
        private float _recovery;

        public HealBuff(float seconds = 5f)
        {
            duration = Math.Max(0f, seconds);
        }

        /// <summary>
        /// 남은 효과 시간만 회복하고 소수 회복량은 다음 갱신으로 이월한다.
        /// </summary>
        public override void UpdateTick(float deltaTime)
        {
            _recovery += Math.Min(Math.Max(0f, deltaTime), RemainingSeconds) * 10f;
            int amount = (int)(_recovery + 0.0001f);
            _recovery -= amount;
            if (owner != null && !owner.IsDead && amount > 0)
            {
                owner.StatusInfo.AddHp(amount);
            }
            base.UpdateTick(deltaTime);
        }
    }
}
