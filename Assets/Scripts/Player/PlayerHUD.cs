using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD Instance;

    [Header("HP 관련")]
    [SerializeField] private Slider hpSlider;

    [Header("EXP 관련")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private GameObject levelUpPanel;

    [Header("이펙트 효과")]
    [SerializeField] private GameObject levelUpEffectPrefab; // 레벨업 이펙트
    [SerializeField] private Transform playerTransform; // 이펙트 터질 위치

    [Header("레벨 관련")]
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("킬 카운트")]
    [SerializeField] private TextMeshProUGUI killCountText;
    private int kills = 0;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateHp(float currentHp, float maxHp)
    {
        if (hpSlider != null) hpSlider.value = currentHp / maxHp;
    }

    public void UpdateExp(float currentExp, float maxExp)
    {
        if (expSlider != null) expSlider.value = currentExp / maxExp;
    }

    public void UpdateLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv.{level}";
        }
    }

    public void AddKill()
    {
        kills++;

        if (killCountText != null)
        {
            killCountText.text = $"Kills : {kills}";
        }
    }

    // 레벨업 시퀀스
    public void StartLevelUpSequence()
    {
        StartCoroutine(LevelUpRoutine());
    }

    // 시간차 연출 로직
    IEnumerator LevelUpRoutine()
    {
        // 이펙트
        if (levelUpEffectPrefab != null && playerTransform != null)
        {
            GameObject effect = Instantiate(levelUpEffectPrefab, playerTransform.position, Quaternion.identity);

            Destroy(effect, 3.0f);
        }

        // 잠시 대기
        yield return new WaitForSeconds(0.5f);

        // UI 켜기
        if (levelUpPanel != null) levelUpPanel.SetActive(true);

        // 일시 정지
        Time.timeScale = 0f;
    }

    // 선택 완료 시퀀스 (버튼에 연결할 함수)
    public void SelectOption()
    {
        StartCoroutine(SelectOptionRoutine());
    }

    IEnumerator SelectOptionRoutine()
    {
        // 레벨업 사운드 여기서 실행

        // UI 끄기
        if (levelUpPanel != null) levelUpPanel.SetActive(false);

        // 잠시 대기
        yield return new WaitForSecondsRealtime(0.5f);

        // 게임 재개
        Time.timeScale = 1f;
    }
}