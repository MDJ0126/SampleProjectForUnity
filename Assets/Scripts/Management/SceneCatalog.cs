using UnityEngine;

namespace Game
{
    /// <summary>
    /// 씬 식별자와 전환 정의의 대응을 보관한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Scene/Scene Catalog")]
    public class SceneCatalog : ScriptableObject
    {
        public SceneDefinition[] entries = new SceneDefinition[0];

        /// <summary>
        /// 중복되거나 비어 있는 씬 식별자는 조회를 거부한다.
        /// </summary>
        public SceneDefinition Find(string id)
        {
            SceneDefinition found = null;
            if (string.IsNullOrWhiteSpace(id) || entries == null)
            {
                return null;
            }
            foreach (SceneDefinition entry in entries)
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
