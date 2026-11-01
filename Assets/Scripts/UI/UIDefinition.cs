using System;
using UnityEngine.AddressableAssets;

namespace Game
{
    /// <summary>
    /// UI의 주소와 표시 계층 및 정리 범위를 정의한다.
    /// </summary>
    [Serializable]
    public class UIDefinition
    {
        public string id;
        public AssetReferenceGameObject prefab;
        public eUILayer layer = eUILayer.Screen;
        public eUILifetime lifetime = eUILifetime.Scene;
        public bool isModal;
        public bool canCloseWithBack = true;
    }
}
