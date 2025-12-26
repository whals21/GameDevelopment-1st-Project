using System.Collections.Generic;
using UnityEngine;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance;

    [Header("데이터")]
    [SerializeField] private ItemData[] allItems; // 액티브 + 패시브

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

        // 배울 수 있는 무기,패시브
        List<ItemData> candidates = GetValidItems();

        // 셔플
        for (int i = 0; i < candidates.Count; i++)
        {
            ItemData temp = candidates[i];
            int rand = Random.Range(i, candidates.Count);
            candidates[i] = candidates[rand];
            candidates[rand] = temp;
        }

        // 버튼에 표시
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

        foreach (var item in allItems)
        {
            // 액티브 스킬
            if (item.itemType == ItemType.Active)
            {
                if (item.skillData == null) continue;

                int currentLv = 0;
                if (SkillManager.Instance != null)
                    currentLv = SkillManager.Instance.GetSkillLevel(item.skillData);

                if (currentLv < item.skillData.levels.Length)
                    validList.Add(item);
            }
            // 패시브 스킬
            else
            {
                int currentLv = 0;
                if (PassiveSkillManager.Instance != null)
                {
                    currentLv = PassiveSkillManager.Instance.GetSkillLevel(item.passiveType);
                }

                // 만렙 체크
                int maxLevel = (item.passiveAmounts != null) ? item.passiveAmounts.Length : 5;

                if (currentLv < maxLevel)
                {
                    validList.Add(item);
                }
            }
        }
        return validList;
    }

    public void SelectItem(ItemData selectedItem)
    {
        // 액티브 -> 스킬 매니저
        if (selectedItem.itemType == ItemType.Active)
        {
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.UnlockOrUpgradeSkill(selectedItem.skillData);
            }
        }
        // 패시브 -> 패시브 매니저
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
}