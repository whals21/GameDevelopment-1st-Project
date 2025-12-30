using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class RouletteManager : MonoBehaviour
{
    public static RouletteManager Instance;

    [System.Serializable]
    public class RewardItem
    {
        public string name; // 이름
        public Sprite icon; // 이미지
        public int goldAmount; // 금액
    }

    [Header("UI 연결")]
    public GameObject roulettePanel; // 룰렛 창 전체
    public GameObject highlighter; // 노란색 테두리
    public List<Transform> slots; // 돌아갈 슬롯
    public Button startButton; // 시작 버튼
    public TextMeshProUGUI resultText; // 결과 텍스트
    public GameObject resultPopup; // 결과 보여주는 팝업창

    [Header("보상 설정")]
    public List<RewardItem> possibleRewards; // 가능한 보상 목록
    private int[] currentSlotGold; // 칸에 각각 얼마 들어갈지

    [Header("설정")]
    public float spinDuration = 3.0f; // 얼마나 오래 돌지
    private bool isSpinning = false; // 회전여부


    private void Awake() { Instance = this; }

    void Start()
    {
        roulettePanel.SetActive(false);
        resultPopup.SetActive(false);
        highlighter.SetActive(false);
        currentSlotGold = new int[slots.Count]; // 초기화
    }

    // 룰렛 창 열기
    public void ShowRoulette()
    {
        roulettePanel.SetActive(true);
        resultPopup.SetActive(false);
        highlighter.SetActive(false);
        startButton.interactable = true; // 버튼
        Time.timeScale = 0f; // 게임 일시정지
        SetRandomSlots(); // 섞기
    }

    void SetRandomSlots()
    {        
        // 슬롯 16개를 돌면서 하나씩 채우기
        for (int i = 0; i < slots.Count; i++)
        {
            // 랜덤으로 보상 하나 뽑기 (0번 ~ 목록개수)
            int randIndex = Random.Range(0, possibleRewards.Count);
            RewardItem pick = possibleRewards[randIndex];

            // 슬롯의 이미지를 바꿈
            Image slotImage = slots[i].GetComponent<Image>();
            if (slotImage != null)
            {
                slotImage.sprite = pick.icon;
            }

            // 칸안에 금액 기록
            currentSlotGold[i] = pick.goldAmount;
        }
    }

    public void OnClickStart()
    {
        if (isSpinning) return;

        startButton.interactable = false; // 도는 동안 버튼 못 누르게
        StartCoroutine(SpinRoutine());
    }

    //돌아버리는 로직
    IEnumerator SpinRoutine()
    {
        isSpinning = true;
        highlighter.SetActive(true);

        int currentIndex = 0; // 현재 불 들어온 칸 번호
        float elapsed = 0f; // 흐른 시간
        float speed = 0.05f; // 불빛 이동 속도 (낮을수록 빠름)

        while (elapsed < spinDuration)
        {
            // 하이라이트를 현재 슬롯 위치로 이동
            highlighter.transform.position = slots[currentIndex].transform.position;

            // 다음 칸으로 번호 이동
            currentIndex++;
            if (currentIndex >= slots.Count) currentIndex = 0;

            // 시간 계산 (점점 느려지게 하고 싶으면 speed를 늘리면 됨)
            elapsed += speed;
            yield return new WaitForSecondsRealtime(speed);

            // 시간이 지날수록 조금씩 느려지게 연출 (선택사항)
            if (elapsed > spinDuration * 0.7f) speed += 0.01f;
        }

        // 결과창 띄우기
        ShowResult(currentIndex);

        isSpinning = false;
    }

    void ShowResult(int slotIndex)
    {
        int rewardGold = currentSlotGold[slotIndex];

        if (DataManager.Instance != null)
        {
            DataManager.Instance.AddGold(rewardGold);
        }

        resultPopup.SetActive(true);
        resultText.text = $"축하합니다\n{rewardGold} 골드 획득!";
    }

    public void OnClickClose()
    {
        roulettePanel.SetActive(false);
        Time.timeScale = 1f;
    }
}