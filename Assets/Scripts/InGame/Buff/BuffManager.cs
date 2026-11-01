using System.Collections.Generic;

namespace Game
{
    public class BuffManager
    {
        private Pawn _owner = null;

        public List<Buff> Buffs { get; private set; } = new();

        /// <summary>
        /// 관리 중인 버프가 적용될 소유 Pawn 설정
        /// </summary>
        public void SetOwner(Pawn owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// 등록된 모든 버프의 지속 시간 및 효과 갱신
        /// </summary>
        public void UpdateTick(float deltaTime)
        {
            for (int i = Buffs.Count - 1; i >= 0; i--)
            {
                Buffs[i].UpdateTick(deltaTime);
            }
        }

        /// <summary>
        /// 버프에 소유자를 연결하고 종료 이벤트를 구독한 뒤 목록에 추가
        /// </summary>
        public void AddBuff(Buff buff)
        {
            if (buff == null || Buffs.Contains(buff))
            {
                return;
            }
            buff.SetOwner(_owner);
            buff.OnEndBuff += RemoveBuff;
            Buffs.Add(buff);
        }

        /// <summary>
        /// 종료된 버프를 관리 목록에서 제거
        /// </summary>
        public void RemoveBuff(Buff buff)
        {
            Buffs.Remove(buff);
        }
    }
}
