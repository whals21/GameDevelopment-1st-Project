using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SkillHUD : MonoBehaviour
{
    public static SkillHUD Instance;

    [Header("슬롯이 들어갈 자리")]
    public Transform weaponRow;  // 윗줄 (Active)
    public Transform passiveRow; // 아랫줄 (Passive)

    [Header("프리팹")]
    public GameObject slotPrefab; // 스킬슬롯

    private Dictionary<string, GameObject> createdSlots = new Dictionary<string, GameObject>();

    void Awake()
    {
        Instance = this;
    }

    // skillType: 0이면 Active, 1이면 Passive
    public void UpdateSkillUI(string skillName, Sprite icon, int level, int skillType)
    {
        // 이미 있는 스킬인지 확인
        if (createdSlots.ContainsKey(skillName))
        {
            // 이미 있으면 레벨 숫자만 업데이트
            GameObject existingSlot = createdSlots[skillName];

            Transform textObj = existingSlot.transform.Find("Skill_Level");
            if (textObj != null)
            {
                TextMeshProUGUI levelText = textObj.GetComponent<TextMeshProUGUI>();
                levelText.text = $"{level}";
            }
        }
        else
        {
            // 없으면 새로 슬롯 만들기
            Transform parentRow = (skillType == 0) ? weaponRow : passiveRow;

            // 슬롯 생성
            GameObject newSlot = Instantiate(slotPrefab, parentRow);

            if (skillType == 1)
            {
                // 180도 회전
                newSlot.transform.localRotation = Quaternion.Euler(0, 0, 180);
            }

            // 아이콘 설정
            Transform iconObj = newSlot.transform.Find("Skill_Icon");
            if (iconObj != null)
            {
                Image iconImg = iconObj.GetComponent<Image>();
                iconImg.sprite = icon;
            }

            // 레벨 설정
            Transform levelObj = newSlot.transform.Find("Skill_Level");
            if (levelObj != null)
            {
                TextMeshProUGUI levelText = levelObj.GetComponent<TextMeshProUGUI>();
                levelText.text = $"{level}";
            }

            // 리스트에 등록
            createdSlots.Add(skillName, newSlot);
        }
    }
}