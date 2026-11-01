using System;

namespace Game
{
    /// <summary>
    /// 체력을 유효 범위로 제한하고 실제 변경만 알린다.
    /// </summary>
    public class StatusInfo
    {
        public event Action<StatusInfo> OnChangedHp;
        public Status baseStatus;
        public int hp { get; private set; }
        public int MaxHp => Math.Max(0, baseStatus.maxHp);
        public float HpRatio => MaxHp > 0 ? (float)hp / MaxHp : 0f;
        public bool IsDead => hp <= 0;

        /// <summary>
        /// 기본 최대 체력으로 초기화하고 화면 갱신을 알린다.
        /// </summary>
        public void Initialize()
        {
            hp = MaxHp;
            OnChangedHp?.Invoke(this);
        }

        /// <summary>
        /// 현재 체력을 0과 최대 체력 사이로 제한한다.
        /// </summary>
        public void SetHp(int value)
        {
            value = Math.Clamp(value, 0, MaxHp);
            if (hp == value)
            {
                return;
            }
            hp = value;
            OnChangedHp?.Invoke(this);
        }

        /// <summary>
        /// 정수 덧셈이 넘치지 않도록 체력 증감을 적용한다.
        /// </summary>
        public void AddHp(int amount)
        {
            SetHp((int)Math.Clamp((long)hp + amount, 0L, MaxHp));
        }

        /// <summary>
        /// 양수 피해만 적용하여 음수 피해로 회복되지 않게 한다.
        /// </summary>
        public void Damaged(int damage)
        {
            if (damage > 0)
            {
                AddHp(-damage);
            }
        }
    }
}
