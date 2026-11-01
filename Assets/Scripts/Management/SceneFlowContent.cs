using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    /// <summary>
    /// 씬의 카메라·입력 수명과 스플래시의 다음 씬 진입을 연결한다.
    /// </summary>
    public class SceneFlowContent : SceneContent
    {
        public Camera sceneCamera;
        public AudioListener sceneListener;
        public PlayerController playerController;
        public PlayerInput playerInput;
        public PlayerCameraController gameplayCameraController;
        public string nextSceneId;
        public float splashDuration = 2.5f;
        public bool startWithMenu = true;
        public Transform previewCharacter;
        public Character gameplayCharacter;
        public string screenId;
        public UIView screenView;
        private UIManager _ui;
        private bool _isMenuOpen;
        private Coroutine _advance;
        public bool IsMenuOpen => _isMenuOpen;

        private void Awake()
        {
            _isMenuOpen = startWithMenu;
            SetPresentationEnabled(false);
            SetGameplayEnabled(false);
        }

        private void Update()
        {
            if (!IsGameplayEnabled || playerController == null)
            {
                return;
            }
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame && !_ui.IsGameplayInputBlocked)
            {
                ToggleMenu();
            }
        }

        private void OnDestroy()
        {
            if (playerController != null)
            {
                playerController.OnPossessed -= OnPawnPossessed;
            }
            if (_ui != null)
            {
                _ui.OnGameplayInputBlockedChanged -= OnInputBlockedChanged;
            }
        }

        /// <summary>
        /// 공통 UI의 입력 차단과 씬에 배치한 화면의 소유권을 연결한다.
        /// </summary>
        public override Task PrepareAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_ui != null)
            {
                _ui.OnGameplayInputBlockedChanged -= OnInputBlockedChanged;
            }
            _ui = UIManager.Instance;
            _ui.OnGameplayInputBlockedChanged += OnInputBlockedChanged;
            if (playerController != null)
            {
                playerController.OnPossessed -= OnPawnPossessed;
                playerController.OnPossessed += OnPawnPossessed;
            }
            if (!_ui.RegisterSceneView(screenId, screenView))
            {
                throw new System.InvalidOperationException("씬에 배치한 화면 등록에 실패했습니다: " + screenId);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 전환·메뉴 상태에 따라 게임 입력과 스플래시 흐름을 제어한다.
        /// </summary>
        public override void SetGameplayEnabled(bool isEnabled)
        {
            base.SetGameplayEnabled(isEnabled);
            ApplyInputState();
            if (isEnabled && !string.IsNullOrEmpty(nextSceneId) && _advance == null)
            {
                _advance = StartCoroutine(AdvanceAfterSplash());
            }
        }

        /// <summary>
        /// 씬 전환 확정 시 카메라와 리스너를 함께 교체한다.
        /// </summary>
        public override void SetPresentationEnabled(bool isEnabled)
        {
            if (sceneCamera != null)
            {
                sceneCamera.enabled = isEnabled;
            }
            if (sceneListener != null)
            {
                sceneListener.enabled = isEnabled;
            }
        }

        /// <summary>
        /// Tab 또는 직렬화된 버튼 이벤트로 게임과 메뉴 입력을 전환한다.
        /// </summary>
        public void ToggleMenu()
        {
            _isMenuOpen = !_isMenuOpen;
            ApplyInputState();
        }

        /// <summary>
        /// 모달과 전환의 차단 상태가 바뀌면 입력 소유권을 다시 적용한다.
        /// </summary>
        private void OnInputBlockedChanged(bool isBlocked)
        {
            ApplyInputState();
        }

        /// <summary>
        /// 기존 빙의 함수가 입력을 켠 직후 현재 메뉴 소유권을 다시 적용한다.
        /// </summary>
        private void OnPawnPossessed(Pawn pawn)
        {
            ApplyInputState();
        }

        /// <summary>
        /// 메뉴와 모달에서는 이동을 멈추고 UI를 클릭할 수 있게 한다.
        /// </summary>
        private void ApplyInputState()
        {
            bool canPlay = IsGameplayEnabled && !_isMenuOpen && _ui != null && !_ui.IsGameplayInputBlocked;
            if (!canPlay && gameplayCharacter != null && gameplayCharacter.Movement != null)
            {
                gameplayCharacter.Movement.MoveInput = Vector3.zero;
                gameplayCharacter.Movement.IsSprint = false;
            }
            if (playerController != null)
            {
                playerController.enabled = canPlay;
                if (gameplayCameraController != null)
                {
                    gameplayCameraController.enabled = canPlay && playerController.IsPossessed;
                }
                if (playerInput != null)
                {
                    playerInput.enabled = canPlay && playerController.IsPossessed;
                }
            }
            if (IsGameplayEnabled)
            {
                bool isPointerFree = playerController == null || !canPlay;
                Cursor.visible = isPointerFree;
                Cursor.lockState = isPointerFree ? CursorLockMode.None : CursorLockMode.Locked;
            }
        }

        /// <summary>
        /// 스플래시를 표시한 뒤 전환 잠금이 해제되면 다음 씬으로 진입한다.
        /// </summary>
        private IEnumerator AdvanceAfterSplash()
        {
            yield return new WaitForSecondsRealtime(splashDuration);
            while (SceneControlManager.Instance != null && SceneControlManager.Instance.IsTransitioning)
            {
                yield return null;
            }
            if (IsGameplayEnabled && SceneControlManager.Instance != null)
            {
                _ = SceneControlManager.Instance.ChangeSceneAsync(nextSceneId);
            }
        }
    }
}
