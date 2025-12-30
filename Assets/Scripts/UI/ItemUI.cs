using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Image iconImg;
    [SerializeField] private TextMeshProUGUI nameTxt;
    [SerializeField] private TextMeshProUGUI descTxt;

    [Header("별 시스템")]
    [SerializeField] private Image[] stars;

    private ItemData data; // 데이터 저장용

    public void Init(ItemData newItem)
    {
        data = newItem;

        // 아이콘과 이름 표시
        if (iconImg != null) iconImg.sprite = newItem.itemIcon;
        if (nameTxt != null) nameTxt.text = newItem.itemName;

        int nextLevel = 1;

        // 액티브 스킬
        if (newItem.itemType == ItemType.Active)
        {
            int currentLv = 0;
            if (SkillManager.Instance != null && newItem.skillData != null)
                currentLv = SkillManager.Instance.GetSkillLevel(newItem.skillData);

            nextLevel = currentLv + 1;

            if (descTxt != null)
            {
                if (currentLv == 0) descTxt.text = newItem.itemDesc;
                else if (newItem.skillData != null && currentLv < newItem.skillData.levels.Length)
                    descTxt.text = newItem.skillData.levels[currentLv].upgradeDescription;
                else descTxt.text = "Max Level";
            }
        }
        // 패시브 스킬일 때
        else
        {
            int currentLv = 0;
            if (PassiveSkillManager.Instance != null)
                currentLv = PassiveSkillManager.Instance.GetSkillLevel(newItem.passiveType);

            nextLevel = currentLv + 1;

            if (descTxt != null)
            {
                // 설명에 수치 넣기
                float amount = newItem.GetPassiveValue(nextLevel);
                // % 예시
                descTxt.text = string.Format(newItem.itemDesc, amount * 100);
            }
        }

        // 별 그리기
        UpdateStars(nextLevel);
    }

    private void UpdateStars(int level)
    {
        if (stars == null) return;
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
                stars[i].gameObject.SetActive(i < level);
        }
    }

    // 버튼 클릭 시 실행
    public void OnClick()
    {
        if (LevelUpManager.Instance != null)
        {
            LevelUpManager.Instance.SelectItem(data);
        }
    }
}