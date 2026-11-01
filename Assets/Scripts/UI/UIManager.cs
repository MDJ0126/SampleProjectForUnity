using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game
{
    /// <summary>
    /// Addressable UI의 단일 생성과 표시 순서 및 수명별 해제를 관리한다.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private sealed class OpenRequest
        {
            public UIDefinition definition;
            public int sceneVersion;
            public TaskCompletionSource<UIView> completion = new TaskCompletionSource<UIView>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private sealed class OpenView
        {
            public UIDefinition definition;
            public UIView view;
            public AsyncOperationHandle<GameObject> handle;
        }

        public UICatalog catalog;
        public RectTransform screenRoot;
        public RectTransform popupRoot;
        public RectTransform overlayRoot;
        private readonly Dictionary<string, OpenRequest> _pending = new Dictionary<string, OpenRequest>();
        private readonly Dictionary<string, OpenView> _views = new Dictionary<string, OpenView>();
        private readonly List<string> _order = new List<string>();
        private int _sceneVersion;
        private int _inFlightCount;
        private bool _isDestroyed;
        private bool _isTransitionBlocking;
        private bool _isGameplayInputBlocked;

        public static UIManager Instance { get; private set; }
        public int OpenCount => _views.Count;
        public int PendingCount => _pending.Count;
        public int InFlightCount => _inFlightCount;
        public bool IsGameplayInputBlocked => _isGameplayInputBlocked;
        public event Action<bool> OnGameplayInputBlockedChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseTop();
            }
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            foreach (OpenRequest request in new List<OpenRequest>(_pending.Values))
            {
                request.completion.TrySetResult(null);
            }
            _pending.Clear();
            foreach (string id in _order.ToArray())
            {
                Close(id);
            }
            OnGameplayInputBlockedChanged = null;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 씬에 배치된 화면을 재생성하지 않고 씬 수명의 UI로 등록한다.
        /// </summary>
        public bool RegisterSceneView(string id, UIView view)
        {
            UIDefinition definition = catalog != null ? catalog.Find(id) : null;
            if (_isDestroyed || view == null || definition == null || definition.lifetime != eUILifetime.Scene || _pending.ContainsKey(id))
            {
                return false;
            }
            if (_views.TryGetValue(id, out OpenView existing))
            {
                return existing.view == view;
            }
            view.Open(this, id, null);
            _views.Add(id, new OpenView { definition = definition, view = view });
            _order.Add(id);
            UpdateInputBlock();
            return true;
        }

        /// <summary>
        /// 씬 해제나 준비 실패로 파괴된 배치 화면을 관리 목록에서 제거한다.
        /// </summary>
        internal void OnViewDestroyed(UIView view)
        {
            if (view.Id != null && _views.TryGetValue(view.Id, out OpenView existing) && existing.view == view)
            {
                _views.Remove(view.Id);
                _order.Remove(view.Id);
                UpdateInputBlock();
            }
        }

        /// <summary>
        /// 같은 UI의 동시 요청을 합치고 닫힌 요청의 늦은 결과를 폐기한다.
        /// </summary>
        public Task<UIView> OpenAsync(string id, object data = null)
        {
            UIDefinition definition = catalog != null ? catalog.Find(id) : null;
            if (_isDestroyed || definition == null || definition.layer == eUILayer.None || definition.lifetime == eUILifetime.None || definition.prefab == null || !definition.prefab.RuntimeKeyIsValid())
            {
                return Task.FromResult<UIView>(null);
            }
            if (_views.TryGetValue(id, out OpenView existing))
            {
                return Task.FromResult(existing.view);
            }
            if (_pending.TryGetValue(id, out OpenRequest pending))
            {
                return pending.completion.Task;
            }
            OpenRequest request = new OpenRequest { definition = definition, sceneVersion = _sceneVersion };
            _pending.Add(id, request);
            _inFlightCount++;
            CompleteOpenAsync(request, data);
            return request.completion.Task;
        }

        /// <summary>
        /// 로드 핸들을 인스턴스 수명에 연결하고 실패·취소 시 반드시 반환한다.
        /// </summary>
        private async void CompleteOpenAsync(OpenRequest request, object data)
        {
            AsyncOperationHandle<GameObject> handle = default;
            UIView createdView = null;
            bool isOwned = false;
            string id = request.definition.id;
            try
            {
                Transform root = GetRoot(request.definition.layer);
                if (root == null)
                {
                    throw new InvalidOperationException("UI 레이어 루트가 연결되지 않았습니다: " + id);
                }
                handle = Addressables.InstantiateAsync(request.definition.prefab.RuntimeKey, root);
                await handle.Task;
                bool isCurrent = _pending.TryGetValue(id, out OpenRequest current) && current == request;
                if (_isDestroyed || !isCurrent || (request.definition.lifetime == eUILifetime.Scene && request.sceneVersion != _sceneVersion))
                {
                    return;
                }
                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    throw new InvalidOperationException("UI 생성에 실패했습니다: " + id);
                }
                UIView view = handle.Result.GetComponent<UIView>();
                createdView = view;
                if (view == null)
                {
                    throw new InvalidOperationException("UI 프리팹에 UIView가 없습니다: " + id);
                }
                view.gameObject.SetActive(false);
                view.Open(this, id, data);
                _views.Add(id, new OpenView { definition = request.definition, view = view, handle = handle });
                _order.Add(id);
                isOwned = true;
                view.transform.SetAsLastSibling();
                view.gameObject.SetActive(true);
                UpdateInputBlock();
                request.completion.TrySetResult(view);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _inFlightCount--;
                if (!isOwned && handle.IsValid())
                {
                    if (createdView != null)
                    {
                        try
                        {
                            createdView.NotifyClosed();
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception);
                        }
                    }
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Addressables.ReleaseInstance(handle);
                    }
                    else
                    {
                        Addressables.Release(handle);
                    }
                }
                if (_pending.TryGetValue(id, out OpenRequest current) && current == request)
                {
                    _pending.Remove(id);
                }
                request.completion.TrySetResult(null);
            }
        }

        /// <summary>
        /// 레이어에 대응하는 Canvas 하위 루트를 반환한다.
        /// </summary>
        private RectTransform GetRoot(eUILayer layer)
        {
            switch (layer)
            {
                case eUILayer.Screen:
                {
                    return screenRoot;
                }
                case eUILayer.Popup:
                {
                    return popupRoot;
                }
                case eUILayer.Overlay:
                {
                    return overlayRoot;
                }
                default:
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 열려 있는 UI만 조회하며 새 에셋을 로드하지 않는다.
        /// </summary>
        public UIView Find(string id)
        {
            return id != null && _views.TryGetValue(id, out OpenView entry) ? entry.view : null;
        }

        /// <summary>
        /// 대기 요청도 무효화하고 인스턴스와 Addressables 참조를 함께 해제한다.
        /// </summary>
        public void Close(string id)
        {
            if (id == null)
            {
                return;
            }
            if (_pending.TryGetValue(id, out OpenRequest request))
            {
                _pending.Remove(id);
                request.completion.TrySetResult(null);
            }
            if (!_views.TryGetValue(id, out OpenView entry))
            {
                return;
            }
            _views.Remove(id);
            _order.Remove(id);
            try
            {
                if (entry.view != null)
                {
                        // 파괴가 프레임 끝까지 지연되어도 이전 UI가 클릭을 가로채지 않게 한다.
                        entry.view.gameObject.SetActive(false);
                    entry.view.NotifyClosed();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (entry.handle.IsValid())
                {
                    Addressables.ReleaseInstance(entry.handle);
                }
                UpdateInputBlock();
            }
        }

        /// <summary>
        /// 전환 중 뒤로 가기를 막고 닫기 가능한 최상단 UI 하나를 해제한다.
        /// </summary>
        public bool CloseTop()
        {
            if (_isTransitionBlocking)
            {
                return false;
            }
            string topId = null;
            eUILayer topLayer = eUILayer.None;
            for (int index = _order.Count - 1; index >= 0; index--)
            {
                OpenView entry = _views[_order[index]];
                if (entry.definition.layer > topLayer && (entry.definition.canCloseWithBack || entry.definition.isModal))
                {
                    topId = _order[index];
                    topLayer = entry.definition.layer;
                }
            }
            if (topId == null || !_views[topId].definition.canCloseWithBack)
            {
                return false;
            }
            Close(topId);
            return true;
        }

        /// <summary>
        /// 씬 수명의 UI와 대기 로드를 무효화하고 전역 UI는 유지한다.
        /// </summary>
        public void CloseSceneViews()
        {
            CloseSceneViewsExcept(null);
        }

        /// <summary>
        /// 준비가 끝난 새 화면 하나를 보존하며 이전 씬 수명을 정리한다.
        /// </summary>
        public void CloseSceneViewsExcept(string retainedId)
        {
            _sceneVersion++;
            foreach (string id in new List<string>(_pending.Keys))
            {
                if (id != retainedId && _pending[id].definition.lifetime == eUILifetime.Scene)
                {
                    Close(id);
                }
            }
            foreach (string id in _order.ToArray())
            {
                if (id != retainedId && _views[id].definition.lifetime == eUILifetime.Scene)
                {
                    Close(id);
                }
            }
        }

        /// <summary>
        /// 전환과 모달의 차단 상태를 합쳐 게임 입력 구독자에게 전달한다.
        /// </summary>
        public void SetTransitionBlocking(bool isBlocking)
        {
            _isTransitionBlocking = isBlocking;
            UpdateInputBlock();
        }

        /// <summary>
        /// 마지막 차단 소유자가 사라진 경우에만 게임 입력을 복원한다.
        /// </summary>
        private void UpdateInputBlock()
        {
            bool isBlocked = _isTransitionBlocking;
            foreach (OpenView entry in _views.Values)
            {
                isBlocked |= entry.definition.isModal;
            }
            if (_isGameplayInputBlocked == isBlocked)
            {
                return;
            }
            _isGameplayInputBlocked = isBlocked;
            if (OnGameplayInputBlockedChanged != null)
            {
                foreach (Action<bool> callback in OnGameplayInputBlockedChanged.GetInvocationList())
                {
                    try
                    {
                        callback(isBlocked);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }
        }
    }
}
