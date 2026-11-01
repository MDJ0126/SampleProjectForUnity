using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Game
{
    [DefaultExecutionOrder(100)]
    public class RobotKyle : Character
    {
        #region Inspector

        public Transform leftHandleSocket;
        public Transform rightHandleSocket;
        public RigBuilder gunRigBuilder;
        public TwoBoneIKConstraint rightArmIK;
        public TwoBoneIKConstraint leftArmIK;
        public MultiAimConstraint rightHandAim;
        public bool isGunAimDebugEnabled;
        [SerializeField] private LineRenderer _gunAimDebugLine;

        #endregion



        public RobotKyleAnimationController RobotKyleAnimationController { get; private set; }
        private Gun _rigGun;


        protected override void Awake()
        {
            RobotKyleAnimationController = GetComponentInChildren<RobotKyleAnimationController>();
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            SkillManager.AddSkill(new RoarSkill());
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            UpdateGunAimDebugLine();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_gunAimDebugLine != null)
            {
                _gunAimDebugLine.enabled = false;
            }
        }

        /// <summary>
        /// 애니메이션과 조준 갱신 후 현재 총구와 조준점의 월드 좌표를 표시한다.
        /// </summary>
        private void UpdateGunAimDebugLine()
        {
            if (_gunAimDebugLine == null)
            {
                return;
            }
            Gun gun = EquippedWeapon as Gun;
            bool isVisible = isGunAimDebugEnabled && gun != null && gun.Muzzle != null && gun.gameObject.activeInHierarchy && aimTarget != null && aimTarget.target != null;
            _gunAimDebugLine.enabled = isVisible;
            if (!isVisible)
            {
                return;
            }
            _gunAimDebugLine.useWorldSpace = true;
            _gunAimDebugLine.positionCount = 2;
            _gunAimDebugLine.SetPosition(0, gun.Muzzle.position);
            _gunAimDebugLine.SetPosition(1, aimTarget.target.position);
        }

        /// <summary>
        /// 장착한 총의 그립으로 왼손 연결을 바꾸고 바인딩을 다시 생성한다.
        /// </summary>
        protected override void OnEquippedWeaponChanged()
        {
            base.OnEquippedWeaponChanged();
            Gun gun = IsWeaponDrawn ? EquippedWeapon as Gun : null;
            if (!IsDead && gunRigBuilder != null)
            {
                gunRigBuilder.enabled = true;
            }
            if (leftArmIK != null)
            {
                var data = leftArmIK.data;
                data.target = gun != null ? gun.LeftGripTarget : null;
                leftArmIK.data = data;
                leftArmIK.weight = data.target != null ? 1f : 0f;
            }
            if (rightArmIK != null)
            {
                rightArmIK.weight = gun != null ? 1f : 0f;
            }
            if (rightHandAim != null)
            {
                rightHandAim.weight = gun != null ? 1f : 0f;
            }
            if (EquippedWeapon != null && RobotKyleAnimationController != null)
            {
                RobotKyleAnimationController.SetWeaponAnimation(isMelee: EquippedWeapon is MeleeWeapon);
            }
            // Transform 핸들은 그래프 생성 시 바인딩되므로 교체 시에만 다시 구성한다.
            if (Application.isPlaying && gun != _rigGun && gunRigBuilder != null && gunRigBuilder.isActiveAndEnabled)
            {
                gunRigBuilder.Build();
            }
            _rigGun = gun;
        }

        protected override void Initalize()
        {
            StatusInfo.baseStatus = StatusTable.Instance.GetData(0);
        }
    }
}
