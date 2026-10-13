using UnityEngine;

namespace Game
{
    /// <summary>
    /// 총기/화기 클래스
    /// </summary>
    public abstract class Gun : Weapon
    {
        [SerializeField] private Transform _rightGripTarget;
        [SerializeField] private Transform _leftGripTarget;

        public Transform RightGripTarget => _rightGripTarget;
        public Transform LeftGripTarget => _leftGripTarget;
    }
}
