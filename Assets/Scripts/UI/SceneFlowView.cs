using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 프리팹에 저장된 버튼 이벤트와 화면 상태만 연결한다.
    /// </summary>
    public class SceneFlowView : UIView
    {
        public string destinationSceneId;
        public string popupId = "MenuPopup";
        public GameObject gameplayMenu;
        public GameObject equipmentPanel;
        public GameObject costumePanel;
        public Text selectionText;
        private SceneFlowContent _content;

        private void Update()
        {
            if (gameplayMenu != null)
            {
                if (_content == null)
                {
                    _content = FindActiveContent();
                }
                if (_content != null && gameplayMenu.activeSelf != _content.IsMenuOpen)
                {
                    gameplayMenu.SetActive(_content.IsMenuOpen);
                }
            }
        }

        /// <summary>
        /// Inspector의 Button.onClick에서 지정한 씬으로 전환한다.
        /// </summary>
        public async void Navigate()
        {
            try
            {
                await SceneControlManager.Instance.ChangeSceneAsync(destinationSceneId);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// Inspector에서 선택한 팝업 프리팹을 공통 관리자로 연다.
        /// </summary>
        public async void OpenPopup()
        {
            try
            {
                await UIManager.Instance.OpenAsync(popupId);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// 메뉴를 닫고 기존 Playground 캐릭터 제어로 복귀한다.
        /// </summary>
        public void ResumePlay()
        {
            SceneFlowContent content = FindActiveContent();
            if (content != null && content.IsMenuOpen)
            {
                content.ToggleMenu();
            }
        }

        /// <summary>
        /// 미리 배치한 장비와 코스튬 패널의 표시를 전환한다.
        /// </summary>
        public void SelectCategory(int category)
        {
            equipmentPanel.SetActive(category == 0);
            costumePanel.SetActive(category == 1);
            selectionText.text = category == 0 ? "EQUIPMENT / FRONT PREVIEW" : "COSTUME / FRONT PREVIEW";
        }

        /// <summary>
        /// 현재 활성 씬이 소유한 입력·미리보기 상태를 조회한다.
        /// </summary>
        private static SceneFlowContent FindActiveContent()
        {
            Scene active = SceneManager.GetActiveScene();
            foreach (GameObject root in active.GetRootGameObjects())
            {
                SceneFlowContent content = root.GetComponentInChildren<SceneFlowContent>();
                if (content != null)
                {
                    return content;
                }
            }
            return null;
        }
    }
}
