using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    /// <summary>
    /// 플레이어만 카메라·입력·커서에 연결한다.
    /// </summary>
    public abstract class PlayerController : PawnController
    {
        protected PlayerInput _input;
        private PlayerCameraController _camera;
        public static PlayerController Instance { get; private set; }

        protected virtual void Awake()
        {
            Instance = this;
            _input = GetComponent<PlayerInput>();
        }

        protected virtual void Update()
        {
#if UNITY_EDITOR
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            {
                if (IsPossessed)
                {
                    Unpossess();
                }
                else
                {
                    Possess();
                }
            }
#endif
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 빙의 이벤트 전에 입력을 연결하여 씬의 메뉴 차단을 보존한다.
        /// </summary>
        protected override void OnPossessionChanged()
        {
            if (_camera == null && Camera.main != null)
            {
                _camera = Camera.main.GetComponent<PlayerCameraController>();
            }
            if (_camera != null)
            {
                if (IsPossessed)
                {
                    _camera.Bind(possessTarget.GetComponentInChildren<SpringArm>());
                }
                else
                {
                    _camera.Unbind();
                }
            }
            if (_input != null)
            {
                _input.enabled = IsPossessed;
            }
            Cursor.visible = !IsPossessed;
            Cursor.lockState = IsPossessed ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}
