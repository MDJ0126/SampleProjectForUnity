using System;
using UnityEngine;

namespace Game
{
    public static class GameUtils
    {
        private static RaycastHit[] _hits = new RaycastHit[32];

        /// <summary>
        /// 소유자와 공격 트리거를 제외한 가장 가까운 차폐 또는 피격 지점을 찾는다.
        /// </summary>
        public static bool TryRaycast(Ray ray, float distance, LayerMask mask, Pawn owner, out RaycastHit closest)
        {
            int count;
            while ((count = Physics.RaycastNonAlloc(ray, _hits, distance, mask, QueryTriggerInteraction.Collide)) == _hits.Length)
            {
                Array.Resize(ref _hits, _hits.Length * 2);
            }
            closest = default;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider collider = _hits[i].collider;
                if (collider.GetComponentInParent<Pawn>() == owner && owner != null)
                {
                    continue;
                }
                HitBox box = collider.GetComponent<HitBox>();
                if (collider.isTrigger && (box == null || box.Role != HitBox.eHitBoxRole.Hurt || !box.enabled))
                {
                    continue;
                }
                if (_hits[i].distance < nearest)
                {
                    nearest = _hits[i].distance;
                    closest = _hits[i];
                }
            }
            return nearest < float.PositiveInfinity;
        }

        /// <summary>
        /// 버프 생성
        /// </summary>
        /// <param name="buffType"></param>
        /// <returns></returns>
        public static Buff CreateBuff(eBuffType buffType)
        {
            // Enum 값에 대응하는 구체 Buff 인스턴스를 생성하고 지원하지 않는 값은 null을 반환한다.
            switch (buffType)
            {
                case eBuffType.Heal:
                {
                    return new HealBuff();
                }
            }
            return null;
        }
    }
}
