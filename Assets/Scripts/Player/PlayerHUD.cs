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
    public TextMeshProUGUI levelText;       //12/29

    [Header("킬 카운트")]
    public TextMeshProUGUI killCountText;   //12/29

    [Header("게임 상태 UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    [Header("이펙트 UI")]
    [SerializeField] private UnityEngine.UI.Image flashPanel;

    private int kills = 0;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseUI();
        }
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
    public void TriggerFlashEffect()
    {
        // 판넬이 연결되어 있을 때만 작동
        if (flashPanel != null)
        {
            // 코루틴 실행
            StartCoroutine(FlashRoutine());
        }
    }
    IEnumerator FlashRoutine()
    {
        // 순식간에 하얗게 변함
        flashPanel.color = new Color(1, 1, 1, 0.8f);

        // 0.5초 동안 서서히 투명해짐
        float duration = 0.5f; // 지속 시간
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;

            // 시간이 지날수록 알파값(투명도)을 0.8 -> 0으로 줄임
            float alpha = Mathf.Lerp(0.8f, 0f, time / duration);
            flashPanel.color = new Color(1, 1, 1, alpha);

            yield return null;
        }

        // 확실하게 투명하게 만들기
        flashPanel.color = new Color(1, 1, 1, 0f);
    }

}