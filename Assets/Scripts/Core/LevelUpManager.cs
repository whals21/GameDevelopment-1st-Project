using System.Collections.Generic;
using UnityEngine;

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

            // 이미 장착된 스킬이거나 레벨업 가능한 스킬만 추가
            bool isAlreadyEquipped = currentLv > 0;
            bool canLevelUp = currentLv < maxLevel;

            if (isAlreadyEquipped || canLevelUp)
            {
                // 슬롯이 가득 찼고 미장착 스킬이면 제외
                if (!isAlreadyEquipped && currentActiveSkillCount >= MAX_ACTIVE_SKILLS)
                {
                    continue;
                }
                validList.Add(item);
            }

            // 진화 조건 충족 시 진화 스킬 추가
            if (EvolutionChecker.CanEvolve(item.skillData, currentLv))
            {
                // evoItems에서 진화 스킬 ItemData 찾기
                ItemData evoItemData = FindEvoItem(item.skillData.evoSkill);
                if (evoItemData != null && !validList.Contains(evoItemData))
                {
                    validList.Add(evoItemData);
                    Debug.Log($"[LevelUpManager] 진화 스킬 추가: {item.skillData.skillName} → {evoItemData.skillData.skillName}");
                }
                else if (evoItemData == null)
                {
                    Debug.LogWarning($"[LevelUpManager] 진화 스킬 ItemData를 찾을 수 없음: {item.skillData.evoSkill?.skillName}");
                }
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

    /// <summary>
    /// evoItems 배열에서 진화 스킬에 해당하는 ItemData를 찾음
    /// </summary>
    private ItemData FindEvoItem(SkillData evoSkillData)
    {
        if (evoSkillData == null || evoItems == null) return null;

        foreach (var evoItem in evoItems)
        {
            if (evoItem != null && evoItem.skillData == evoSkillData)
            {
                return evoItem;
            }
        }
        return null;
    }

    public void SelectItem(ItemData selectedItem)
    {
        // 액티브 스킬 -> 스킬 매니저
        if (selectedItem.itemType == ItemType.Active)
        {
            if (SkillManager.Instance != null)
            {
                // 진화 스킬인지 확인 (evoItems에 있는지 체크)
                if (IsEvolutionSkill(selectedItem))
                {
                    ExecuteEvolutionForItem(selectedItem);
                }
                else
                {
                    // 일반 스킬 레벨업 또는 장착
                    SkillManager.Instance.UpgradeOrEquipSkill(selectedItem.skillData);
                }
            }
        }
        // 패시브 스킬 -> 패시브 매니저
        else
        {
            if (PassiveSkillManager.Instance != null)
            {
                PassiveSkillManager.Instance.TryAcquireSkill(selectedItem.passiveType);
            }
        }

        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 해당 아이템이 진화 스킬인지 확인
    /// </summary>
    private bool IsEvolutionSkill(ItemData item)
    {
        if (evoItems == null) return false;
        foreach (var evoItem in evoItems)
        {
            if (evoItem == item) return true;
        }
        return false;
    }

    /// <summary>
    /// 진화 스킬 선택 시 기존 스킬을 찾아 진화 실행
    /// </summary>
    private void ExecuteEvolutionForItem(ItemData evoItem)
    {
        if (SkillManager.Instance == null || evoItem.skillData == null) return;

        // 모든 장착 스킬을 순회하며 이 진화 스킬로 진화 가능한 스킬 찾기
        var allSkills = SkillManager.Instance.GetAllSkills();
        foreach (var skill in allSkills)
        {
            if (skill != null && skill.Data != null &&
                skill.Data.evoSkill == evoItem.skillData &&
                EvolutionChecker.CanEvolve(skill.Data, skill.CurrentLevel))
            {
                EvolutionChecker.ExecuteEvolution(skill);
                Debug.Log($"[LevelUpManager] {skill.Data.skillName} → {evoItem.skillData.skillName} 진화 완료!");
                return;
            }
        }

        Debug.LogWarning($"[LevelUpManager] 진화 가능한 기존 스킬을 찾을 수 없습니다: {evoItem.skillData.skillName}");
    }

    /// <summary>
    /// 스킬 이름으로 ItemData를 찾음 (StatisticsUIPanel용)
    /// </summary>
    public ItemData GetItemDataBySkillName(string skillName)
    {
        if (string.IsNullOrEmpty(skillName)) return null;

        // activeItems 검색
        if (activeItems != null)
        {
            foreach (var item in activeItems)
            {
                if (item != null && item.skillData != null && item.skillData.skillName == skillName)
                {
                    return item;
                }
            }
        }

        // evoItems 검색
        if (evoItems != null)
        {
            foreach (var item in evoItems)
            {
                if (item != null && item.skillData != null && item.skillData.skillName == skillName)
                {
                    return item;
                }
            }
        }

        return null;
    }
}
