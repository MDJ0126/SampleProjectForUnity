using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game
{
    /// <summary>
    /// 공통 초기화 프리팹을 한 번만 생성하고 씬을 넘어 유지한다.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class SceneUIInitializer : MonoBehaviour
    {
        public SceneControlManager sceneControl;
        private static Task<SceneUIInitializer> _pending;
        private static int _generation;
        private AsyncOperationHandle<GameObject> _instanceHandle;
        private bool _ownsHandle;
        public static SceneUIInitializer Instance { get; private set; }
        public static int CreatedRootCount { get; private set; }

        private void Awake()
        {
            CreatedRootCount++;
            if (Instance != null && Instance != this)
            {
                // 자식 매니저와 EventSystem의 초기화 전에 중복 루트를 비활성화한다.
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            if (_ownsHandle && _instanceHandle.IsValid())
            {
                _ownsHandle = false;
                Addressables.ReleaseInstance(_instanceHandle);
            }
        }

        /// <summary>
        /// Domain Reload를 끈 Play에서도 이전 정적 요청을 이어받지 않게 한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Instance = null;
            _pending = null;
            CreatedRootCount = 0;
            _generation++;
        }

        /// <summary>
        /// 기존 루트와 진행 중 로드를 공유하여 여러 씬의 동시 초기화를 합친다.
        /// </summary>
        public static Task<SceneUIInitializer> EnsureAsync(AssetReferenceGameObject prefab)
        {
            if (Instance != null)
            {
                return Task.FromResult(Instance);
            }
            if (_pending != null)
            {
                return _pending;
            }
            if (prefab == null || !prefab.RuntimeKeyIsValid())
            {
                return Task.FromResult<SceneUIInitializer>(null);
            }
            TaskCompletionSource<SceneUIInitializer> completion = new TaskCompletionSource<SceneUIInitializer>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending = completion.Task;
            CreateAsync(prefab, completion, _generation);
            return completion.Task;
        }

        /// <summary>
        /// 초기화 인스턴스 핸들을 루트에 넘기고 실패·이전 Play의 결과는 해제한다.
        /// </summary>
        private static async void CreateAsync(AssetReferenceGameObject prefab, TaskCompletionSource<SceneUIInitializer> completion, int generation)
        {
            AsyncOperationHandle<GameObject> handle = default;
            bool isOwned = false;
            try
            {
                handle = Addressables.InstantiateAsync(prefab.RuntimeKey);
                await handle.Task;
                if (generation != _generation || !Application.isPlaying)
                {
                    return;
                }
                SceneUIInitializer initializer = handle.Result.GetComponent<SceneUIInitializer>();
                if (initializer == null || Instance != initializer)
                {
                    throw new InvalidOperationException("공통 초기화 프리팹의 루트를 확인해야 합니다.");
                }
                initializer._instanceHandle = handle;
                initializer._ownsHandle = true;
                isOwned = true;
                completion.TrySetResult(initializer);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (!isOwned && handle.IsValid())
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Addressables.ReleaseInstance(handle);
                    }
                    else
                    {
                        Addressables.Release(handle);
                    }
                }
                if (_pending == completion.Task)
                {
                    _pending = null;
                }
                completion.TrySetResult(null);
            }
        }
    }
}
