using UnityEngine;
using UnityEngine.UI;

public class ScienceNode : MonoBehaviour
{
    // 윗줄(Stat)인지 아랫줄(Special)인지 구분
    public enum NodeType { Stat, Special }

    [Header("기본 데이터 설정")]
    public int nodeId; // 고유 번호
    public NodeType nodeType; // 타입 (Stat: 윗줄, Special: 아랫줄)
    public string skillName; // 스킬 이름
    [TextArea] public string desc; // 설명

    [Header("가격 및 효과")]
    public int price; // 가격
    public float value; // 증가 수치

    [Header("해금 조건 (아랫줄용)")]
    public int requiredLevel;      // 윗줄이 몇 단계여야 해금되는가?

    [Header("UI 연결 (드래그 앤 드롭)")]
    public Button myButton; // 버튼
    public Image iconImage; // 아이콘
    public GameObject lockObj; // 자물쇠 이미지
    public GameObject purchasedObj;// 구매 완료 표시

    public void UpdateState(int currentTopLevel, bool isMySpecialUnlocked)
    {
        bool isUnlocked = false;
        bool isPurchased = false;

        // 상태 판별 로직
        if (nodeType == NodeType.Stat) // 윗줄
        {
            if (nodeId < currentTopLevel) isPurchased = true;
            else if (nodeId == currentTopLevel) isUnlocked = true;
        }
        else // 아랫줄
        {
            if (isMySpecialUnlocked) isPurchased = true;
            else if (currentTopLevel >= requiredLevel) isUnlocked = true;
        }

        // UI 반영 로직
        if (isPurchased)
        {
            iconImage.color = Color.white;
            if (lockObj != null) lockObj.SetActive(false);
            if (purchasedObj != null) purchasedObj.SetActive(true);
            myButton.interactable = false; // 이미 산 건 클릭 금지
        }
        else if (isUnlocked)
        {
            iconImage.color = Color.white;
            if (lockObj != null) lockObj.SetActive(false);
            if (purchasedObj != null) purchasedObj.SetActive(false);
            myButton.interactable = true; // 구매 가능
        }
        else // 잠김
        {
            iconImage.color = Color.gray;
            if (lockObj != null) lockObj.SetActive(true);
            if (purchasedObj != null) purchasedObj.SetActive(false);
            myButton.interactable = true;
        }
    }
}