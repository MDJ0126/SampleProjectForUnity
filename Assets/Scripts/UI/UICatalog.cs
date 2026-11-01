using UnityEngine;

namespace Game
{
    /// <summary>
    /// UI 식별자와 Addressable 프리팹의 대응을 보관한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/UI/UI Catalog")]
    public class UICatalog : ScriptableObject
    {
        public UIDefinition[] entries = new UIDefinition[0];

        /// <summary>
        /// 중복되거나 비어 있는 식별자는 조회를 거부한다.
        /// </summary>
        public UIDefinition Find(string id)
        {
            UIDefinition found = null;
            if (string.IsNullOrWhiteSpace(id) || entries == null)
            {
                return null;
            }
            foreach (UIDefinition entry in entries)
            {
                if (entry != null && entry.id == id)
                {
                    if (found != null)
                    {
                        return null;
                    }
                    found = entry;
                }
            }
            return found;
        }
    }
}
