using UnityEngine;
using System.Collections.Generic;

public class StatisticsUIPanel : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private GameObject slotPrefab; // 슬롯프리팹
    [SerializeField] private Transform contentTransform; // Scroll View 안의 Content

    private void OnEnable()
    {
        // 패널이 켜질 때 자동으로 갱신
        UpdateUI();
    }

    public void UpdateUI()
    {
        // 기존 슬롯 청소
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        if (StatisticsManager.Instance == null) return;

        // 통계 데이터 가져오기
        float gameTime = 0f;
        if (GameManager.Instance != null) gameTime = GameManager.Instance.GameTime;

        List<SkillSessionData> report = StatisticsManager.Instance.GetReport(gameTime);

        if (report.Count == 0) return;

        // 1등 딜량 찾기
        float maxDamage = report[0].totalDamage;

        // 슬롯 생성하기
        foreach (var data in report)
        {
            GameObject newSlot = Instantiate(slotPrefab, contentTransform);
            StatisticsUISlot slotScript = newSlot.GetComponent<StatisticsUISlot>();

            if (slotScript != null)
            {
                // 데이터 받아오기
                ItemData foundItem = null;
                if (LevelUpManager.Instance != null)
                {
                    foundItem = LevelUpManager.Instance.GetItemDataBySkillName(data.skillName);
                }

                // 이름과 아이콘 결정하기
                string displayName = data.skillName; // 기본값은 영어
                Sprite displayIcon = null;

                if (foundItem != null)
                {
                    displayName = foundItem.itemName; // 한글 이름 가져옴
                    displayIcon = foundItem.itemIcon; // 아이콘
                }

                slotScript.Init(data, maxDamage, displayIcon, displayName);
            }
        }
    }
}