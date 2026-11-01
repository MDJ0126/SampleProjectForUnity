using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Game
{
    /// <summary>
    /// Addressable 씬 전환을 직렬화하고 초기화 실패 시 이전 씬을 복원한다.
    /// </summary>
    public class SceneControlManager : MonoBehaviour
    {
        public SceneCatalog catalog;
        public UIManager uiManager;
        public string loadingUIId = "Loading";
        public string errorUIId = "Error";
        public float minimumLoadingSeconds = 0.35f;
        private AsyncOperationHandle<SceneInstance> _currentHandle;
        private Scene _currentScene;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private bool _isDestroyed;
        public static SceneControlManager Instance { get; private set; }
        public bool IsTransitioning { get; private set; }
        public string CurrentSceneId { get; private set; }
        public string LastError { get; private set; }
        public event Action<string> OnSceneChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            _lifetime.Cancel();
            _lifetime.Dispose();
            OnSceneChanged = null;
            if (_currentHandle.IsValid() && _currentHandle.IsDone && _currentHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Addressables.UnloadSceneAsync(_currentHandle);
                _currentHandle = default;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 에디터나 플레이어가 직접 연 씬을 재로드하지 않고 현재 씬으로 준비한다.
        /// </summary>
        public async Task<bool> InitializeLoadedSceneAsync(string sceneId, Scene scene)
        {
            SceneDefinition definition = catalog != null ? catalog.Find(sceneId) : null;
            if (_isDestroyed || IsTransitioning || CurrentSceneId != null || definition == null || !scene.IsValid() || !scene.isLoaded || uiManager == null)
            {
                return false;
            }
            IsTransitioning = true;
            LastError = null;
            uiManager.SetTransitionBlocking(true);
            try
            {
                if (await uiManager.OpenAsync(loadingUIId) == null)
                {
                    throw new InvalidOperationException("로딩 UI를 생성할 수 없습니다.");
                }
                List<SceneContent> contents = GetContents(scene);
                SetGameplay(contents, false);
                foreach (SceneContent content in contents)
                {
                    await content.PrepareAsync(_lifetime.Token);
                }
                if (!string.IsNullOrEmpty(definition.screenId) && await uiManager.OpenAsync(definition.screenId, sceneId) == null)
                {
                    throw new InvalidOperationException("초기 화면을 생성할 수 없습니다.");
                }
                _lifetime.Token.ThrowIfCancellationRequested();
                _currentScene = scene;
                CurrentSceneId = sceneId;
                SetPresentation(contents, true);
                SetGameplay(contents, true);
                NotifySceneChanged(sceneId);
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                Debug.LogWarning("[SceneControl] " + LastError);
                if (!_isDestroyed && uiManager != null)
                {
                    uiManager.CloseSceneViews();
                    await uiManager.OpenAsync(errorUIId, LastError);
                }
                return false;
            }
            finally
            {
                IsTransitioning = false;
                if (uiManager != null)
                {
                    uiManager.Close(loadingUIId);
                    uiManager.SetTransitionBlocking(false);
                    uiManager.Find(catalog.Find(CurrentSceneId)?.screenId)?.RefreshLayout();
                }
            }
        }

        /// <summary>
        /// 중복·동일·알 수 없는 씬 요청은 로드 없이 거부한다.
        /// </summary>
        public async Task<bool> ChangeSceneAsync(string sceneId)
        {
            SceneDefinition definition = catalog != null ? catalog.Find(sceneId) : null;
            if (_isDestroyed || IsTransitioning || CurrentSceneId == sceneId || definition == null || definition.scene == null || !definition.scene.RuntimeKeyIsValid() || uiManager == null)
            {
                return false;
            }
            IsTransitioning = true;
            LastError = null;
            uiManager.SetTransitionBlocking(true);
            AsyncOperationHandle<SceneInstance> nextHandle = default;
            Scene previousScene = _currentScene.IsValid() ? _currentScene : SceneManager.GetActiveScene();
            List<SceneContent> previousContents = _currentScene.IsValid() && _currentScene.isLoaded ? GetContents(_currentScene) : new List<SceneContent>();
            bool isCommitted = false;
            UIView preparedScreen = null;
            try
            {
                SetGameplay(previousContents, false);
                UIView loading = await uiManager.OpenAsync(loadingUIId);
                if (loading == null)
                {
                    throw new InvalidOperationException("로딩 UI를 생성할 수 없습니다.");
                }
                SetLoading(0.1f, "Loading scene");
                float loadingStartedAt = Time.realtimeSinceStartup;
                nextHandle = Addressables.LoadSceneAsync(definition.scene.RuntimeKey, LoadSceneMode.Additive, true);
                await nextHandle.Task;
                _lifetime.Token.ThrowIfCancellationRequested();
                if (nextHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw new InvalidOperationException("씬 로드에 실패했습니다: " + sceneId);
                }
                if (!SceneManager.SetActiveScene(nextHandle.Result.Scene))
                {
                    throw new InvalidOperationException("활성 씬을 지정할 수 없습니다: " + sceneId);
                }
                SetLoading(0.55f, "Preparing scene");
                List<SceneContent> nextContents = GetContents(nextHandle.Result.Scene);
                foreach (SceneContent content in nextContents)
                {
                    await content.PrepareAsync(_lifetime.Token);
                }
                float remaining = minimumLoadingSeconds - (Time.realtimeSinceStartup - loadingStartedAt);
                if (remaining > 0f)
                {
                    await Task.Delay(Mathf.CeilToInt(remaining * 1000f), _lifetime.Token);
                }
                _lifetime.Token.ThrowIfCancellationRequested();
                // 초기 화면 생성 실패도 이전 씬이 남아 있는 단계에서 확인한다.
                UIView nextScreen = null;
                if (!string.IsNullOrEmpty(definition.screenId))
                {
                    nextScreen = await uiManager.OpenAsync(definition.screenId, sceneId);
                    preparedScreen = nextScreen;
                    if (nextScreen == null)
                    {
                        throw new InvalidOperationException("초기 화면을 생성할 수 없습니다: " + definition.screenId);
                    }
                }
                // 준비된 새 화면만 남기며 이전 씬의 UI와 대기 요청을 정리한다.
                uiManager.CloseSceneViewsExcept(definition.screenId);
                SetLoading(0.85f, "Releasing previous scene");
                AsyncOperationHandle<SceneInstance> previousHandle = _currentHandle;
                _currentHandle = nextHandle;
                _currentScene = nextHandle.Result.Scene;
                nextHandle = default;
                CurrentSceneId = sceneId;
                isCommitted = true;
                SetPresentation(previousContents, false);
                SetPresentation(nextContents, true);
                if (previousHandle.IsValid())
                {
                    await Addressables.UnloadSceneAsync(previousHandle).Task;
                }
                else if (previousScene.IsValid() && previousScene.isLoaded)
                {
                    // 직접 Play로 연 첫 씬은 Addressables 핸들이 없으므로 네이티브 경로로 정리한다.
                    AsyncOperation unload = SceneManager.UnloadSceneAsync(previousScene);
                    while (unload != null && !unload.isDone)
                    {
                        await Task.Yield();
                    }
                }
                SetGameplay(nextContents, true);
                SetLoading(1f, "Ready");
                NotifySceneChanged(sceneId);
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                if (!isCommitted)
                {
                    if (preparedScreen != null)
                    {
                        preparedScreen.Close();
                    }
                    if (previousScene.IsValid() && previousScene.isLoaded)
                    {
                        SceneManager.SetActiveScene(previousScene);
                    }
                    if (nextHandle.IsValid())
                    {
                        if (nextHandle.Status == AsyncOperationStatus.Succeeded)
                        {
                            await Addressables.UnloadSceneAsync(nextHandle).Task;
                        }
                        else
                        {
                            Addressables.Release(nextHandle);
                        }
                    }
                    SetGameplay(previousContents, true);
                }
                else if (!_isDestroyed && _currentHandle.IsValid())
                {
                    SetGameplay(GetContents(_currentHandle.Result.Scene), true);
                }
                Debug.LogWarning("[SceneControl] " + LastError);
                if (!_isDestroyed && uiManager != null)
                {
                    await uiManager.OpenAsync(errorUIId, LastError);
                }
                return false;
            }
            finally
            {
                IsTransitioning = false;
                if (uiManager != null)
                {
                    uiManager.Close(loadingUIId);
                    uiManager.SetTransitionBlocking(false);
                    uiManager.Find(catalog.Find(CurrentSceneId)?.screenId)?.RefreshLayout();
                }
            }
        }

        /// <summary>
        /// 구독자 예외가 이미 완료된 씬 전환의 성공 상태를 바꾸지 않게 한다.
        /// </summary>
        private void NotifySceneChanged(string sceneId)
        {
            if (OnSceneChanged == null)
            {
                return;
            }
            foreach (Action<string> callback in OnSceneChanged.GetInvocationList())
            {
                try
                {
                    callback(sceneId);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        /// <summary>
        /// 지정 씬 내부의 준비 계약만 수집하여 다른 씬의 상태를 보존한다.
        /// </summary>
        private static List<SceneContent> GetContents(Scene scene)
        {
            List<SceneContent> contents = new List<SceneContent>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                contents.AddRange(root.GetComponentsInChildren<SceneContent>(true));
            }
            return contents;
        }

        /// <summary>
        /// 수집한 씬 계약에 게임플레이 활성 상태를 전달한다.
        /// </summary>
        private static void SetGameplay(List<SceneContent> contents, bool isEnabled)
        {
            foreach (SceneContent content in contents)
            {
                if (content != null)
                {
                    content.SetGameplayEnabled(isEnabled);
                }
            }
        }

        /// <summary>
        /// 확정 전후의 씬 계약에 카메라·오디오 활성 상태를 전달한다.
        /// </summary>
        private static void SetPresentation(List<SceneContent> contents, bool isEnabled)
        {
            foreach (SceneContent content in contents)
            {
                if (content != null)
                {
                    content.SetPresentationEnabled(isEnabled);
                }
            }
        }

        /// <summary>
        /// 다운로드 백분율과 구분되는 씬 전환 단계 진행도를 표시한다.
        /// </summary>
        private void SetLoading(float progress, string message)
        {
            if (uiManager.Find(loadingUIId) is LoadingUIView loading)
            {
                loading.SetProgress(progress, message);
            }
        }
    }
}
