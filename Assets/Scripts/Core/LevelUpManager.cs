using System.Collections.Generic;
using UnityEngine;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance;

    [Header("데이터 설정")]
    [SerializeField] private ItemData[] activeItems;
    [SerializeField] private ItemData[] passiveItems;

    [Header("UI 연결")]
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

        // 배울 수 있는 스킬 가져오기
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

        // 액티브 스킬 목록 검사
        foreach (var item in activeItems)
        {
            // 실수로 패시브를 넣었을 경우를 대비한 안전장치
            if (item.itemType != ItemType.Active) continue;
            if (item.skillData == null) continue;

            int currentLv = 0;
            if (SkillManager.Instance != null)
                currentLv = SkillManager.Instance.GetSkillLevel(item.skillData);

            // 만렙 체크
            int maxLevel = item.skillData.levels.Length;

            if (currentLv < maxLevel)
            {
                validList.Add(item);
            }
        }

        // 패시브 스킬 목록 검사
        foreach (var item in passiveItems)
        {
            // 실수로 액티브를 넣었을 경우를 대비한 안전장치
            if (item.itemType != ItemType.Passive) continue;

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