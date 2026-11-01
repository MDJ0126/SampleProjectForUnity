namespace Game
{
    public class HealSkill : Skill
    {
        /// <summary>
        /// 소유 Pawn의 현재 체력 10 회복
        /// </summary>
        public override void Execute()
        {
            if (owner != null && !owner.IsDead)
            {
                owner.StatusInfo.AddHp(10);
            }
        }
    }
}
