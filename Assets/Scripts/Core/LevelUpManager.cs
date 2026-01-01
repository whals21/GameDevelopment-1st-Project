using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance;

    [Header("스킬 아이템 데이터")]
    [SerializeField] private ItemData[] activeItems;
    [SerializeField] private ItemData[] passiveItems;
    [SerializeField] private ItemData[] evoItems;  // 진화 스킬

    [Header("UI 설정")]
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private ItemUI[] itemButtons;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowLevelUp()
    {
        levelUpPanel.SetActive(true);
        Time.timeScale = 0f;

        // ��� �� �ִ� ��ų ��������
        List<ItemData> candidates = GetValidItems();

        // ����
        for (int i = 0; i < candidates.Count; i++)
        {
            ItemData temp = candidates[i];
            int rand = Random.Range(i, candidates.Count);
            candidates[i] = candidates[rand];
            candidates[rand] = temp;
        }

        // ��ư�� ǥ��
        for (int i = 0; i < itemButtons.Length; i++)
        {
            if (i < candidates.Count)
            {
                itemButtons[i].gameObject.SetActive(true);
                itemButtons[i].Init(candidates[i]);
            }
            else
            {
                itemButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private List<ItemData> GetValidItems()
    {
        List<ItemData> validList = new List<ItemData>();

        // 현재 장착된 액티브 스킬 개수 확인
        int currentActiveSkillCount = 0;
        const int MAX_ACTIVE_SKILLS = 6;
        if (SkillManager.Instance != null)
        {
            currentActiveSkillCount = SkillManager.Instance.GetSkillCount();
        }

        // 액티브 스킬만 검색
        foreach (var item in activeItems)
        {
            // 아이템 타입이 액티브인 것만 추가
            if (item.itemType != ItemType.Active) continue;
            if (item.skillData == null) continue;

            int currentLv = 0;
            if (SkillManager.Instance != null)
                currentLv = SkillManager.Instance.GetSkillLevel(item.skillData);

            // 레벨 체크
            int maxLevel = item.skillData.levels.Length;

            // 이미 장착된 스킬이거나 레벨업/진화 가능한 스킬만 추가
            bool isAlreadyEquipped = currentLv > 0;
            bool canLevelUp = currentLv < maxLevel;
            bool canEvolve = EvolutionChecker.CanEvolve(item.skillData, currentLv);

            if (isAlreadyEquipped || canLevelUp || canEvolve)
            {
                // 슬롯이 가득 찼고 미장착 스킬이면 제외
                if (!isAlreadyEquipped && currentActiveSkillCount >= MAX_ACTIVE_SKILLS)
                {
                    continue;
                }
                validList.Add(item);
            }
        }

        // 현재 장착된 패시브 스킬 개수 확인
        int currentPassiveSkillCount = 0;
        const int MAX_PASSIVE_SKILLS = 6;
        if (PassiveSkillManager.Instance != null)
        {
            currentPassiveSkillCount = PassiveSkillManager.Instance.GetAllOwnedSkills().Count;
        }

        // 패시브 스킬만 검색
        foreach (var item in passiveItems)
        {
            // 아이템 타입이 패시브인 것만 추가
            if (item.itemType != ItemType.Passive) continue;

            int currentLv = 0;
            if (PassiveSkillManager.Instance != null)
            {
                currentLv = PassiveSkillManager.Instance.GetSkillLevel(item.passiveType);
            }

            // 레벨 체크
            int maxLevel = (item.passiveAmounts != null) ? item.passiveAmounts.Length : 5;

            // 이미 장착된 스킬이거나 레벨업 가능한 스킬만 추가
            bool isAlreadyEquipped = currentLv > 0;
            bool canLevelUp = currentLv < maxLevel;

            if (isAlreadyEquipped || canLevelUp)
            {
                // 슬롯이 가득 찼고 미장착 스킬이면 제외
                if (!isAlreadyEquipped && currentPassiveSkillCount >= MAX_PASSIVE_SKILLS)
                {
                    continue;
                }
                validList.Add(item);
            }
        }

        return validList;
    }

    public void SelectItem(ItemData selectedItem)
    {
        int currentLevel = 1; // LV 표시

        // 엑티브 선택시
        if (selectedItem.itemType == ItemType.Active)
        {
            if (SkillManager.Instance != null)
            {
                // 획득하거나 강화함
                SkillManager.Instance.UpgradeOrEquipSkill(selectedItem.skillData);

                // UI 갱신
                currentLevel = SkillManager.Instance.GetSkillLevel(selectedItem.skillData);
            }

            // 윗줄에 알려줄 코드
            if (SkillHUD.Instance != null)
            {
                SkillHUD.Instance.UpdateSkillUI(selectedItem.itemName, selectedItem.itemIcon, currentLevel, 0);
            }
        }
        // 패시브 선택시
        else
        {
            if (PassiveSkillManager.Instance != null)
            {
                // 획득, 강화
                PassiveSkillManager.Instance.TryAcquireSkill(selectedItem.passiveType);

                // 갱신
                currentLevel = PassiveSkillManager.Instance.GetSkillLevel(selectedItem.passiveType);
            }

            // 아랫줄에 알려줄 코드
            if (SkillHUD.Instance != null)
            {
                SkillHUD.Instance.UpdateSkillUI(selectedItem.itemName, selectedItem.itemIcon, currentLevel, 1);
            }
        }

        // 게임 재개
        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 스킬 진화 처리
    /// </summary>
    private void EvolveSkill(SkillData baseSkill)
    {
        if (baseSkill == null || baseSkill.evoSkill == null) return;

        SkillData evoSkill = baseSkill.evoSkill;

        // 기존 스킬 제거하고 진화 스킬로 교체
        SkillManager.Instance.ReplaceSkill(baseSkill, evoSkill, 1);

        Debug.Log($"[LevelUpManager] {baseSkill.skillName} → {evoSkill.skillName} 진화 완료!");
    }

    public ItemData GetItemDataBySkillName(string searchName)
    {
        // 액티브 목록 검색
        if (activeItems != null)
        {
            foreach (var item in activeItems)
            {
                // 이름이 영어 이름과 같은지 확인
                if (item.skillData != null && item.skillData.skillName == searchName)
                {
                    return item; // 데이터 반환
                }
            }
        }

        return null; // 못 찾음
    }
}
