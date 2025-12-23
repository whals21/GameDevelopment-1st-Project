using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

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

    [Header("게임 상태 UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
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

    // 일시 정지 창 켜기
    public void TogglePauseUI()
    {
        // 토글식
        bool isActive = !pausePanel.activeSelf;
        pausePanel.SetActive(isActive);

        // 창이 켜지면 시간 정지, 꺼지면 시간 재개
        Time.timeScale = isActive ? 0f : 1f;
    }

    // 게임 오버 창 켜기
    public void ShowGameOverUI()
    {
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // 승리 창 켜기
    public void ShowVictoryUI()
    {
        victoryPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // 다시하기 (현재 씬 재로딩)
    public void OnClickRestart()
    {
        Time.timeScale = 1f;
        // 현재 씬의 이름을 가져와서 재시작
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 저장하고 종료
    public void OnClickSaveAndQuit()
    {
        // 저장 후
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

        // 끔
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 그냥 종료
    public void OnClickQuitOnly()
    {
        // 저장 코드 없이 끔
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    // 계속하기
    public void OnClickResume()
    {
        TogglePauseUI();
    }

}