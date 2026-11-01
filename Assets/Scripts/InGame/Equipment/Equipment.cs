using UnityEngine;

namespace Game
{
    /// <summary>
    /// 소유자와 표시 이름만 공통으로 보관한다.
    /// </summary>
    public class Equipment : MonoBehaviour
    {
        public string displayName;
        public Pawn Owner { get; private set; }
        public string DisplayName => string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;

        /// <summary>
        /// 장비 목록을 관리하는 Pawn이 소유권을 연결한다.
        /// </summary>
        public virtual void SetOwner(Pawn owner)
        {
            Owner = owner;
        }
    }
}
