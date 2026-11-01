using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 씬 사이에서 공유하는 플레이어 무기 선택만 보관한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Equipment Loadout")]
    public class EquipmentLoadout : ScriptableObject
    {
        public Weapon[] weapons;
        public int defaultIndex;
        public string selectionId = "Player";
        private static readonly Dictionary<string, int> _selections = new Dictionary<string, int>();
        private string SelectionKey => string.IsNullOrEmpty(selectionId) ? name : selectionId;
        public int SelectedIndex => _selections.TryGetValue(SelectionKey, out int index) ? index : defaultIndex;
        public Weapon SelectedWeapon => weapons != null && SelectedIndex >= 0 && SelectedIndex < weapons.Length ? weapons[SelectedIndex] : null;

        /// <summary>
        /// 목록 범위 안의 선택만 현재 Play의 공유 상태로 저장한다.
        /// </summary>
        public bool Select(int index)
        {
            if (weapons == null || index < 0 || index >= weapons.Length || weapons[index] == null)
            {
                return false;
            }
            _selections[SelectionKey] = index;
            return true;
        }

        /// <summary>
        /// 새 Play에서 이전 실행의 임시 선택을 이어받지 않게 한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSelections()
        {
            _selections.Clear();
        }
    }
}
