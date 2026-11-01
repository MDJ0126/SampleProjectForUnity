using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    /// <summary>
    /// 카메라 기준 이동과 조준 입력을 공통 Pawn 전투에 연결한다.
    /// </summary>
    public class RobotKylePlayerController : PlayerController
    {
        private Character _owner;
        private Vector2 _move;

        protected override void Update()
        {
            base.Update();
            if (_owner == null || !_owner.IsCombatEnabled || _owner.IsDead)
            {
                return;
            }
            Camera camera = Camera.main;
            if (camera != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
                _owner.Movement.MoveInput = forward * _move.y + Vector3.Cross(Vector3.up, forward) * _move.x;
                _owner.Movement.FacingDirection = forward;
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame)
                {
                    _owner.SelectWeapon(0);
                }
                if (keyboard.digit2Key.wasPressedThisFrame)
                {
                    _owner.SelectWeapon(1);
                }
                if (keyboard.qKey.wasPressedThisFrame)
                {
                    _owner.SelectNextWeapon();
                }
                if (keyboard.fKey.wasPressedThisFrame)
                {
                    _owner.SetWeaponDrawn(!_owner.IsWeaponDrawn, true);
                }
                if (keyboard.rKey.wasPressedThisFrame && _owner.EquippedWeapon is Gun gun)
                {
                    gun.Reload();
                }
            }
            InputAction attack = _input != null ? _input.currentActionMap?.FindAction("NormalAttack") : null;
            if (attack != null && attack.IsPressed() && _owner.EquippedWeapon is Gun automatic && automatic.isAutomatic)
            {
                Attack();
            }
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (_owner == null || aimTarget == null || aimTarget.target == null || camera == null)
            {
                return;
            }
            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            aimTarget.target.position = GameUtils.TryRaycast(ray, aimDistance, aimMask, _owner, out RaycastHit hit) ? hit.point : ray.GetPoint(aimDistance);
        }

        private void OnDisable()
        {
            _move = Vector2.zero;
            _owner?.Movement?.StopMove();
        }

        /// <summary>
        /// 빙의 변경 후 입력의 대상을 갱신한다.
        /// </summary>
        protected override void OnPossessionChanged()
        {
            _owner = possessTarget as Character;
            _move = Vector2.zero;
            base.OnPossessionChanged();
        }

        /// <summary>
        /// Input Actions의 가로 입력을 보관한다.
        /// </summary>
        private void OnHorizontal(InputValue value)
        {
            _move.x = value.Get<float>();
        }

        /// <summary>
        /// Input Actions의 세로 입력을 보관한다.
        /// </summary>
        private void OnVertical(InputValue value)
        {
            _move.y = value.Get<float>();
        }

        /// <summary>
        /// 달리기 입력을 이동 컴포넌트에 전달한다.
        /// </summary>
        private void OnSprint(InputValue value)
        {
            if (_owner != null)
            {
                _owner.Movement.IsSprint = value.isPressed;
            }
        }

        /// <summary>
        /// 누른 프레임에만 점프한다.
        /// </summary>
        private void OnJump(InputValue value)
        {
            if (_owner != null && value.isPressed)
            {
                _owner.Movement.Jump();
            }
        }

        /// <summary>
        /// 단발 입력과 자동 사격의 유지 상태를 분리한다.
        /// </summary>
        private void OnNormalAttack(InputValue value)
        {
            if (value.isPressed)
            {
                Attack();
            }
        }

        /// <summary>
        /// 기존 스킬 입력을 유지한다.
        /// </summary>
        private void OnSkill1(InputValue value)
        {
            if (_owner != null && _owner.IsCombatEnabled && !_owner.IsDead && value.isPressed)
            {
                _owner.SkillManager.UseSkill(0);
            }
        }

        /// <summary>
        /// 조준점과 공통 공격 조건을 사용한다.
        /// </summary>
        private void Attack()
        {
            if (_owner != null && aimTarget != null && aimTarget.target != null)
            {
                _owner.TryAttack(aimTarget.target.position);
            }
        }
    }
}
