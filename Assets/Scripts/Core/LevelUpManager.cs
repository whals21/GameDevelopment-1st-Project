using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance;

    [Header("������ ����")]
    [SerializeField] private ItemData[] activeItems;
    [SerializeField] private ItemData[] passiveItems;

    [Header("UI ����")]
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

        // ��Ƽ�� ��ų ��� �˻�
        foreach (var item in activeItems)
        {
            // �Ǽ��� �нú긦 �־��� ��츦 ����� ������ġ
            if (item.itemType != ItemType.Active) continue;
            if (item.skillData == null) continue;

            int currentLv = 0;
            if (SkillManager.Instance != null)
                currentLv = SkillManager.Instance.GetSkillLevel(item.skillData);

            // ���� üũ
            int maxLevel = item.skillData.levels.Length;

            if (currentLv < maxLevel)
            {
                validList.Add(item);
            }
            // 2. 진화 가능한 스킬 (최대 레벨 달성 + 진화 조건 충족)_EvolutionChecker.CanEvolve 사용_1230 조민희수정
            else if (EvolutionChecker.CanEvolve(item.skillData, currentLv))
            {
                validList.Add(item);
            }
        }

        // �нú� ��ų ��� �˻�
        foreach (var item in passiveItems)
        {
            // �Ǽ��� ��Ƽ�긦 �־��� ��츦 ����� ������ġ
            if (item.itemType != ItemType.Passive) continue;

            int currentLv = 0;
            if (PassiveSkillManager.Instance != null)
            {
                currentLv = PassiveSkillManager.Instance.GetSkillLevel(item.passiveType);
            }

            // ���� üũ
            int maxLevel = (item.passiveAmounts != null) ? item.passiveAmounts.Length : 5;

            if (currentLv < maxLevel)
            {
                validList.Add(item);
            }
        }

        return validList;
    }

    public void SelectItem(ItemData selectedItem)
    {
        // ��Ƽ�� -> ��ų �Ŵ���
        if (selectedItem.itemType == ItemType.Active)
        {
            if (SkillManager.Instance != null)
            {
                // UpgradeOrEquipSkill 사용_1230 조민희수정
                SkillManager.Instance.UpgradeOrEquipSkill(selectedItem.skillData);
            }
        }
        // �нú� -> �нú� �Ŵ���
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
