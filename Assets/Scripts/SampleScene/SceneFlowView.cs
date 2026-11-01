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
        public EquipmentLoadout loadout;
        public Text combatText;
        public GameObject aimReticle;
        private SceneFlowContent _content;

        private void Update()
        {
            if (_content == null)
            {
                _content = FindContent();
            }
            if (aimReticle != null && _content != null)
            {
                Pawn pawn = _content.gameplayCharacter;
                aimReticle.SetActive(pawn != null && pawn.IsCombatEnabled && !pawn.IsDead && pawn.IsWeaponDrawn && pawn.EquippedWeapon is Gun);
            }
            if (combatText != null && _content != null)
            {
                Pawn pawn = _content.gameplayCharacter;
                Weapon weapon = pawn != null ? pawn.EquippedWeapon : null;
                Gun gun = weapon as Gun;
                combatText.text = pawn == null ? "" : (pawn.IsDead ? "DEFEATED - RESTART FROM MENU" : "HP " + pawn.StatusInfo.hp + "/" + pawn.StatusInfo.MaxHp + "  |  " + (weapon != null ? weapon.DisplayName : "UNARMED") + (pawn.IsWeaponDrawn ? "" : " [SHEATHED]") + (gun != null ? "  " + gun.Ammo + "/" + gun.Reserve + (gun.IsReloading ? " RELOADING" : "") : ""));
            }
            if (gameplayMenu != null)
            {
                if (_content == null)
                {
                    _content = FindContent();
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
            SceneFlowContent content = FindContent();
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
        /// 직렬화한 장비 버튼으로 미리보기와 다음 씬의 공통 선택을 바꾼다.
        /// </summary>
        public void SelectWeapon(int index)
        {
            if (loadout == null || !loadout.Select(index))
            {
                return;
            }
            SceneFlowContent content = FindContent();
            Pawn preview = content != null && content.previewCharacter != null ? content.previewCharacter.GetComponent<Pawn>() : null;
            if (preview != null)
            {
                preview.SelectWeapon(index);
            }
            if (selectionText != null)
            {
                selectionText.text = "EQUIPMENT / " + loadout.SelectedWeapon.DisplayName.ToUpperInvariant();
            }
        }

        /// <summary>
        /// Inspector 이벤트를 기존 게임 모드의 재시작에 연결한다.
        /// </summary>
        public void RestartDuel()
        {
            SceneFlowContent content = FindContent();
            if (content != null)
            {
                content.RestartDuel();
            }
        }

        /// <summary>
        /// 배치한 화면과 같은 씬의 입력·미리보기 상태를 조회한다.
        /// </summary>
        private SceneFlowContent FindContent()
        {
            Scene active = gameObject.scene;
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
