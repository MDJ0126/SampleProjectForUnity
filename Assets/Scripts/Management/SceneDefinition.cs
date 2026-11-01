using System;
using UnityEngine.AddressableAssets;

namespace Game
{
    /// <summary>
    /// 이동할 씬의 Addressable 참조와 초기 화면을 정의한다.
    /// </summary>
    [Serializable]
    public class SceneDefinition
    {
        public string id;
        public AssetReference scene;
        public string screenId;
    }
}
