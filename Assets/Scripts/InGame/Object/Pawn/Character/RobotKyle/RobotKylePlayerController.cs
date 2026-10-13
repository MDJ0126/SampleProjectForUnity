using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class RobotKylePlayerController : PlayerController
    {
        private RobotKyle _owner = null;
        private float horizontal;
        private float vertical;

        public override void Possess(Pawn pawn = null)
        {
            base.Possess(pawn);
            _owner = possessTarget as RobotKyle;
            _owner.isGunAimDebugEnabled = true;
        }

        public override void Unpossess()
        {
            base.Unpossess();
            _owner.isGunAimDebugEnabled = false;
        }

        /// <summary>
        /// 가로 이동 입력 이벤트
        /// </summary>
        /// <param name="value"></param>
        private void OnHorizontal(InputValue value)
        {
            horizontal = value.Get<float>();
        }

        /// <summary>
        /// 세로 이동 입력 이벤트
        /// </summary>
        /// <param name="value"></param>
        private void OnVertical(InputValue value)
        {
            vertical = value.Get<float>();
        }

        /// <summary>
        /// 이동 업데이트
        /// </summary>
        private void UpdateMoveInput()
        {
            if (!_owner) return;

            Vector2 input = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);

            Transform cameraTransform = Camera.main.transform;

            Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;

            Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;

            _owner.Movement.MoveInput = cameraForward * input.y + cameraRight * input.x;
        }

        /// <summary>
        /// 전력 질주 입력 이벤트
        /// </summary>
        /// <param name="value"></param>
        private void OnSprint(InputValue value)
        {
            if (!_owner) return;

            _owner.Movement.IsSprint = value.isPressed;
        }

        /// <summary>
        /// 점프 입력 이벤트
        /// </summary>
        /// <param name="value"></param>
        private void OnJump(InputValue value)
        {
            if (!_owner) return;

            _owner.Movement.Jump();
        }

        /// <summary>
        /// 기본 공격 입력 이벤트
        /// </summary>
        private void OnNormalAttack(InputValue value)
        {
            if (!_owner) return;

            _owner.RobotKyleAnimationController.StartAttack();
        }

        /// <summary>
        /// 스킬1 입력 이벤트
        /// </summary>
        /// <param name="value"></param>
        private void OnSkill1(InputValue value)
        {
            if (!_owner) return;

            _owner.SkillManager.UseSkill(0);
        }

        private void Update()
        {
            UpdateMoveInput();
        }

        /// <summary>
        /// 카메라 이동 후 화면 중앙을 기준으로 에임 타겟 갱신
        /// </summary>
        private void LateUpdate()
        {
            UpdateAimTarget();
        }

        /// <summary>
        /// 에임 타겟 업데이트
        /// </summary>
        private void UpdateAimTarget()
        {
            if (possessTarget == null || aimTarget == null) return;

            // 캐릭터 전방과 에임 방향의 내적을 0~1로 변환해 Aim Rig 가중치로 사용한다.
            //Vector3 direction = (aimTarget.target.position - possessTarget.Transform.position).normalized;
            //float dot = Vector3.Dot(possessTarget.Transform.forward, direction);
            //float value01 = Mathf.InverseLerp(-1f, 1f, dot);
            //aimTarget.AimRig.weight = value01;

            // 화면 중앙 Ray가 맞은 지점을 사용하고, 없으면 최대 조준 거리를 사용한다.
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (Physics.Raycast(ray, out var hit, aimDistance, aimMask))
            {
                aimTarget.target.position = hit.point;
            }
            else
            {
                aimTarget.target.position = ray.GetPoint(aimDistance);
            }
        }
    }
}