using System.Collections.Generic;
using UnityEngine;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance;

    [Header("아이템")]
    [SerializeField] private ItemData[] allItems;

    [Header("UI")]
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

        // 레벨업 가능한 모든 아이템 가져오기
        List<ItemData> candidates = GetValidItems();

        // 후보 중 3개 랜덤 선택하여 버튼에 표시
        int count = Mathf.Min(3, candidates.Count);

        // 후보 섞기 (셔플)
        ShuffleList(candidates);

        for (int i = 0; i < itemButtons.Length; i++)
        {
            if (i < count)
            {
                itemButtons[i].gameObject.SetActive(true);
                ItemData item = candidates[i];

                // 현재 스킬 레벨 확인
                int currentLv = 0;
                if (SkillManager.Instance != null)
                    currentLv = SkillManager.Instance.GetSkillLevel(item.skillData);

                // 다음 레벨
                int nextLv = currentLv + 1;

                // 설명 텍스트 생성
                string desc = GetItemDescription(item, currentLv);

                itemButtons[i].SetItem(item, nextLv, desc);
            }
            else
            {
                // 남는 버튼 숨기기
                itemButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 아이템 설명 텍스트 생성
    /// </summary>
    private string GetItemDescription(ItemData item, int currentLv)
    {
        // 진화 가능한 스킬인지 확인
        if (CanEvolveTo(item.skillData))
        {
            SkillData evoSkill = item.skillData.evoSkill;
            return $"진화: {item.skillData.skillName} → {evoSkill.skillName}\n{evoSkill.description}";
        }

        // 일반 레벨업
        if (currentLv == 0)
        {
            // 처음 획득 시 기본 설명
            return item.itemDesc;
        }
        else
        {
            // 이미 가지고 있으면 레벨업 시 설명
            if (item.skillData != null && currentLv < item.skillData.levels.Length)
                return item.skillData.levels[currentLv].upgradeDescription;
            else
                return "Max Level";
        }
    }

    /// <summary>
    /// 진화 가능 여부 확인 - 1229 수정 조민희
    /// v2: 데이터 주도형 진화 확인 (EvolutionChecker.CanEvolve 사용)
    /// </summary>
    private bool CanEvolveTo(SkillData skillData)
    {
        if (skillData == null || skillData.evoSkill == null) return false;

        // 이미 진화 스킬을 가지고 있으면 진화 불가
        if (SkillManager.Instance.GetSkillLevel(skillData.evoSkill) > 0) return false;

        // 현재 스킬이 최고 레벨이어야 진화 가능
        int currentLv = SkillManager.Instance.GetSkillLevel(skillData);
        if (currentLv < skillData.levels.Length) return false;

        // v2: 데이터 주도형 진화 확인 (EvolutionChecker 사용)
        return EvolutionChecker.CanEvolve(skillData, currentLv);
    }

    // 레벨업 가능한 모든 아이템,스킬 확인
    private List<ItemData> GetValidItems()
    {
        List<ItemData> validList = new List<ItemData>();

        foreach (var item in allItems)
        {
            if (item.skillData == null) continue;

            // 현재 레벨 확인
            int currentLv = 0;
            if (SkillManager.Instance != null)
                currentLv = SkillManager.Instance.GetSkillLevel(item.skillData);

            // 1. 일반 스킬 레벨업 (최대 레벨 미달)
            if (currentLv < item.skillData.levels.Length)
            {
                validList.Add(item);
            }
            // 2. 진화 가능한 스킬 (최대 레벨 달성 + 진화 조건 충족)
            else if (CanEvolveTo(item.skillData))
            {
                validList.Add(item);
            }
        }
        return validList;
    }

    // 셔플
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void SelectItem(ItemData selectedItem)
    {
        if (SkillManager.Instance != null)
        {
            // 진화 가능한 스킬이면 진화 처리
            if (CanEvolveTo(selectedItem.skillData))
            {
                EvolveSkill(selectedItem.skillData);
            }
            else
            {
                // 일반 스킬 레벨업
                SkillManager.Instance.UpgradeOrEquipSkill(selectedItem.skillData);
            }
        }

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
}
