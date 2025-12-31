using UnityEngine;
using System.IO;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class ScienceManager : MonoBehaviour
{
    [Header("데이터")]
    private ScienceData myData;
    private string savePath;

    [Header("메인 UI")]
    public TextMeshProUGUI goldText;
    public List<ScienceNode> allNodes;

    [Header("팝업 UI")]
    public GameObject popupPanel;
    public TextMeshProUGUI pTitle;
    public TextMeshProUGUI pDesc;
    public TextMeshProUGUI pCost;
    public TextMeshProUGUI pBtnText;

    private ScienceNode selectedNode;

    void Start()
    {
        savePath = Application.persistentDataPath + "/ScienceData.json";
        LoadData();
        UpdateUI();
        popupPanel.SetActive(false);
    }

    void UpdateUI()
    {
        if (goldText != null) goldText.text = $"{myData.gold}";

        foreach (var node in allNodes)
        {
            bool isSpecialBought = myData.purchasedSpecials.Contains(node.nodeId);
            node.UpdateState(myData.topRowLevel, isSpecialBought);
        }
    }

    // 버튼 클릭 연결용
    public void OnClickNode()
    {
        GameObject btn = EventSystem.current.currentSelectedGameObject;
        if (btn == null) return;

        ScienceNode node = btn.GetComponent<ScienceNode>();
        if (node == null) return;

        selectedNode = node;
        Vector3 btnPos = btn.transform.position; // 노드의 현재위치
        popupPanel.transform.position = btnPos + new Vector3(250f, 0f, 0f); // 팝업창 위치 이동
        OpenPopup();
    }

    void OpenPopup()
    {
        popupPanel.SetActive(true);
        pTitle.text = selectedNode.skillName;
        pDesc.text = selectedNode.desc;
        pCost.text = $"{selectedNode.price} G";

        // 구매 가능 여부 확인
        bool canBuy = false;
        if (selectedNode.nodeType == ScienceNode.NodeType.Stat)
        {
            if (selectedNode.nodeId == myData.topRowLevel) canBuy = true;
        }
        else
        {
            if (myData.topRowLevel >= selectedNode.requiredLevel
                && !myData.purchasedSpecials.Contains(selectedNode.nodeId))
            {
                canBuy = true;
            }
        }

        // 버튼 글자 설정
        if (canBuy)
        {
            if (myData.gold >= selectedNode.price) pBtnText.text = "업그레이드";
            else pBtnText.text = "<color=red>골드 부족</color>";
        }
        else
        {
            if ((selectedNode.nodeType == ScienceNode.NodeType.Stat && selectedNode.nodeId < myData.topRowLevel) ||
                myData.purchasedSpecials.Contains(selectedNode.nodeId))
            {
                pBtnText.text = "완료됨";
            }
            else
            {
                pBtnText.text = "잠김";
            }
        }
    }

    // 팝업 안의 업그레이드 버튼 연결용
    public void OnClickPurchase()
    {
        if (myData.gold < selectedNode.price) return;

        bool isSuccess = false;

        // 윗줄 구매 로직
        if (selectedNode.nodeType == ScienceNode.NodeType.Stat)
        {
            if (selectedNode.nodeId == myData.topRowLevel)
            {
                myData.topRowLevel++;
                isSuccess = true;
            }
        }
        // 아랫줄 구매 로직
        else
        {
            if (myData.topRowLevel >= selectedNode.requiredLevel
                && !myData.purchasedSpecials.Contains(selectedNode.nodeId))
            {
                myData.purchasedSpecials.Add(selectedNode.nodeId);
                isSuccess = true;
            }
        }

        if (isSuccess)
        {
            myData.gold -= selectedNode.price;
            SaveData();
            UpdateUI();
            popupPanel.SetActive(false);
        }
    }

    public void ClosePopup()
    {
        popupPanel.SetActive(false);
    }

    // 초기화 버튼용
    public void OnClickReset()
    {
        int refundAmount = 0;

        // 모든 노드확인, 가격 합산
        foreach (var node in allNodes)
        {
            // 윗줄
            if (node.nodeType == ScienceNode.NodeType.Stat)
            {
                if (node.nodeId < myData.topRowLevel)
                {
                    refundAmount += node.price;
                }
            }
            // 아랫줄
            else
            {
                if (myData.purchasedSpecials.Contains(node.nodeId))
                {
                    refundAmount += node.price;
                }
            }
        }

        // 환불 금액 추가
        myData.gold += refundAmount;

        // 레벨 초기화
        myData.topRowLevel = 0;
        myData.purchasedSpecials.Clear();

        // 저장 및 갱신
        SaveData();
        UpdateUI();
    }

    void SaveData() { File.WriteAllText(savePath, JsonUtility.ToJson(myData, true)); }
    void LoadData()
    {
        if (File.Exists(savePath)) myData = JsonUtility.FromJson<ScienceData>(File.ReadAllText(savePath));
        else myData = new ScienceData();
    }
}