using UnityEngine;

namespace Game
{
    public class WidgetAnchor : MonoBehaviour
    {

        private Transform _transform;

        public Transform Transform
        {
            get
            {
                if (_transform == null)
                    _transform = transform;
                return _transform;
            }
        }

        /// <summary>
        /// WidgetAnchor를 HUD 타겟으로 바로 전달할 수 있도록 Transform으로 변환
        /// </summary>
        public static implicit operator Transform(WidgetAnchor anchor)
        {
            return anchor.Transform;
        }

}
}
