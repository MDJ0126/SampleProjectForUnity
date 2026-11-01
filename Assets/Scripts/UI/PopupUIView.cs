using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 모달 데이터 전달과 인스턴스 단위 닫기를 시연한다.
    /// </summary>
    public class PopupUIView : UIView
    {
        public Text messageText;
        public Button closeButton;

        /// <summary>
        /// 호출자가 전달한 메시지를 표시한다.
        /// </summary>
        protected override void OnOpened(object data)
        {
            if (data is string message)
            {
                messageText.text = message;
            }
        }
    }
}

