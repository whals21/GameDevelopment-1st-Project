using System.Collections.Generic;
using UnityEngine;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance;

    [Header("데이터")]
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

        // 뽑을 수 있는 후보
        List<ItemData> candidates = GetValidItems();

        // 랜덤으로 3개 뽑아서 버튼에 표시
        int count = Mathf.Min(3, candidates.Count); // 후보가 3개보다 적을 수도 있으니까

        // 후보 섞기 (랜덤)
        ShuffleList(candidates);

        for (int i = 0; i < itemButtons.Length; i++)
        {
            if (i < count)
            {
                itemButtons[i].gameObject.SetActive(true);
                ItemData item = candidates[i];

                // 현재 레벨 가져오기
                int currentLv = 0;
                if (SkillManager.Instance != null)
                    currentLv = SkillManager.Instance.GetSkillLevel(item.skillData);

                // 다음 레벨
                int nextLv = currentLv + 1;

                // 설명 가져오기
                string desc = "";
                if (currentLv == 0)
                {
                    // 처음 얻을 땐 기본 설명
                    desc = item.itemDesc;
                }
                else
                {
                    // 업그레이드일 땐 스킬 데이터 안의 '업그레이드 설명'
                    if (item.skillData != null && currentLv < item.skillData.levels.Length)
                        desc = item.skillData.levels[currentLv].upgradeDescription;
                    else
                        desc = "Max Level";
                }

                itemButtons[i].SetItem(item, nextLv, desc);
            }
            else
            {
                // 뽑을 게 없으면 버튼 끄기
                itemButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // 배울수 있는 무기,스킬 확인
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

            // 최대 렙 보다 작아야 배울 수 있음
            if (currentLv < item.skillData.levels.Length)
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
            SkillManager.Instance.UnlockOrUpgradeSkill(selectedItem.skillData);
        }

        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}