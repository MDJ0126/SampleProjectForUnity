using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Game
{
    public class RobotKyle : Character
    {
        #region Inspector

        public Socket leftHandleSocket;
        public Socket rightHandleSocket;
        public RigBuilder gunRigBuilder;
        public TwoBoneIKConstraint rightArmIK;
        public TwoBoneIKConstraint leftArmIK;
        public MultiAimConstraint rightHandAim;

        #endregion

        public override string Name => nameof(RobotKyle);

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

        /// <summary>
        /// 장착한 총의 그립으로 왼손 연결을 바꾸고 바인딩을 다시 생성한다.
        /// </summary>
        protected override void OnEquippedWeaponChanged()
        {
            base.OnEquippedWeaponChanged();
            Gun gun = EquippedWeapon as Gun;
            if (_rigGun != null && _rigGun != gun)
            {
                _rigGun.gameObject.SetActive(false);
            }
            _rigGun = gun;
            if (gun != null && rightHandleSocket != null)
            {
                gun.transform.SetParent(rightHandleSocket.transform, false);
                gun.transform.localPosition = Vector3.zero;
                gun.transform.localRotation = Quaternion.identity;
                gun.gameObject.SetActive(true);
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
            if (Application.isPlaying && gunRigBuilder != null && gunRigBuilder.isActiveAndEnabled)
            {
                gunRigBuilder.Build();
            }
        }

        protected override void Initalize()
        {
            Status status = StatusTable.Instance.GetData(0);
            StatusInfo.baseStatus += status;
            StatusInfo.Initialize();

            if (EquippedWeapon != null)
            {
                RobotKyleAnimationController.SetWeaponAnimation(isMelee: EquippedWeapon is MeleeWeapon);
            }
        }

        protected override void DamagerProcess(Pawn attacker, DamageInfo damageInfo)
        {
            this.StatusInfo.Damaged(damageInfo.damage);
            RobotKyleAnimationController.OnHitted();
        }
    }
}
