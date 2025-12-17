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

        // 3개 랜덤 뽑기
        ItemData[] randomItems = GetRandomItems(3);

        // 버튼에 정보 추가
        for (int i = 0; i < itemButtons.Length; i++)
        {
            itemButtons[i].SetItem(randomItems[i]);
        }
    }

    // 아이템을 선택했을 때 실행되는 함수
    public void SelectItem(ItemData selectedItem)
    {

        // 스킬매니저 호출
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.UnlockOrUpgradeSkill(selectedItem.skillData);
        }

        // 창 닫고 게임 재개
        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // 랜덤 뽑기 로직 (중복 방지)
    private ItemData[] GetRandomItems(int count)
    {
        List<ItemData> tempList = new List<ItemData>(allItems);
        ItemData[] results = new ItemData[count];

        for (int i = 0; i < count; i++)
        {
            if (tempList.Count == 0) break;
            int randomIndex = Random.Range(0, tempList.Count);
            results[i] = tempList[randomIndex];
            tempList.RemoveAt(randomIndex);
        }
        return results;
    }
}