using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game
{
    /// <summary>
    /// 현재 연 씬에서 공통 프리팹을 준비하고 직접 재생을 시작한다.
    /// </summary>
    public class SceneEntry : MonoBehaviour
    {
        public string sceneId;
        public AssetReferenceGameObject initializationPrefab;

        private async void Start()
        {
            try
            {
                SceneUIInitializer initializer = await SceneUIInitializer.EnsureAsync(initializationPrefab);
                if (initializer == null || this == null)
                {
                    return;
                }
                SceneControlManager control = initializer.sceneControl;
                // 전환으로 들어온 씬은 기존 관리자의 준비 흐름을 그대로 사용한다.
                if (control.CurrentSceneId == null && !control.IsTransitioning)
                {
                    await control.InitializeLoadedSceneAsync(sceneId, gameObject.scene);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}

