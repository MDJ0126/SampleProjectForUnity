using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Game
{
    /// <summary>
    /// 관리자가 소유하는 UI의 열기·닫기와 데이터 연결 계약을 제공한다.
    /// </summary>
    public class UIView : MonoBehaviour
    {
        private UIManager _owner;
        private bool _isClosed;
        public string Id { get; private set; }

        private void OnDestroy()
        {
            if (_owner != null)
            {
                _owner.OnViewDestroyed(this);
            }
            NotifyClosed();
        }

        /// <summary>
        /// 생성이 끝난 UI에 소유자와 표시 데이터를 연결한다.
        /// </summary>
        public void Open(UIManager owner, string id, object data)
        {
            _owner = owner;
            Id = id;
            _isClosed = false;
            OnOpened(data);
        }

        /// <summary>
        /// 상속 UI의 데이터와 이벤트 구독을 연결한다.
        /// </summary>
        protected virtual void OnOpened(object data)
        {
        }

        /// <summary>
        /// 파괴 전에 상속 UI의 데이터와 이벤트 구독을 정리한다.
        /// </summary>
        public virtual void OnClosed()
        {
        }

        /// <summary>
        /// 이전 번들의 해제가 끝난 다음 프레임에 배치 UI의 렌더 데이터를 갱신한다.
        /// </summary>
        public void RefreshLayout()
        {
            StartCoroutine(Refresh());
            IEnumerator Refresh()
            {
                yield return null;
                foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
                {
                    graphic.SetAllDirty();
                }
                Canvas.ForceUpdateCanvases();
            }
        }

        /// <summary>
        /// 닫기와 파괴가 이어져도 데이터 해제 계약은 한 번만 실행한다.
        /// </summary>
        internal void NotifyClosed()
        {
            if (!_isClosed)
            {
                _isClosed = true;
                OnClosed();
            }
        }

        /// <summary>
        /// 동일 식별자의 새 UI를 닫지 않도록 인스턴스 소유권을 확인한다.
        /// </summary>
        public void Close()
        {
            if (_owner != null && _owner.Find(Id) == this)
            {
                _owner.Close(Id);
            }
        }
    }
}
