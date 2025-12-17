using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImg;
    [SerializeField] private TextMeshProUGUI nameTxt;
    [SerializeField] private TextMeshProUGUI descTxt;

    private ItemData data;

    // 매니저가 정보를 넣어주는 함수
    public void SetItem(ItemData newItem)
    {
        data = newItem;
        iconImg.sprite = newItem.itemIcon;
        nameTxt.text = newItem.itemName;
        descTxt.text = newItem.itemDesc;
    }

    // 버튼 클릭 시 실행 (Inspector의 OnClick에 연결해야 함)
    public void OnClick()
    {
        if (LevelUpManager.Instance != null)
        {
            LevelUpManager.Instance.SelectItem(data);
        }
    }
}