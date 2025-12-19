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

    private ItemData data;

    public void SetItem(ItemData newItem, int nextLevel, string desc)
    {
        data = newItem;

        // 기본 정보
        iconImg.sprite = newItem.itemIcon;
        nameTxt.text = newItem.itemName;
        descTxt.text = desc;

        // 별 갯수 조절
        for (int i = 0; i < stars.Length; i++)
        {
            if (i < nextLevel)
            {
                stars[i].gameObject.SetActive(true); // 켜기
            }
            else
            {
                stars[i].gameObject.SetActive(false); // 끄기
            }
        }
    }

    public void OnClick()
    {
        if (LevelUpManager.Instance != null)
        {
            LevelUpManager.Instance.SelectItem(data);
        }
    }
}