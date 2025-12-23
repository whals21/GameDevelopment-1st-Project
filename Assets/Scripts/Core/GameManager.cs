using UnityEngine;
using System;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static GameManager Instance { get; private set; }

    // 플레이어 참조 
    public Transform player; 
    // 레벨업 매니저 참조
    public LevelUpManager levelUpManager;

    [Header("플레이어 데이터")]
    public int level = 1;        // 현재 레벨 (기본 1)
    public float exp = 0;        // 현재 경험치
    public float maxExp = 10;    // 경험치 통 (기본 10)
    public int killCount = 0;    // 킬 수
    
    // 게임 상태 열거형
    public enum GameState
    {
        Playing,
        Paused,
        GameOver,
        Victory
    }

    // 현재 게임 상태
    public GameState CurrentState { get; private set; }

    // 타이머
    public float GameTime { get; private set; }
    public bool IsGameRunning { get; private set; }

    // 이벤트
    public event Action<GameState> OnGameStateChanged;
    public event Action<float> OnGameTimeUpdated;

    [Header("드랍경험치")] //<----12/17 추가함
    public DataManager dataManager;
    public int currentTierIndex = 0;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartGame();

        // 2. 이어하기인지 확인!
        if (SaveManager.Instance != null && SaveManager.Instance.isContinue)
        {
            // ★ 이어하기라면: 저장된 데이터를 내 변수에 덮어씌우기
            level = SaveManager.Instance.nowPlayer.level;
            exp = SaveManager.Instance.nowPlayer.currentExp;
            maxExp = SaveManager.Instance.nowPlayer.maxExp;
            killCount = SaveManager.Instance.nowPlayer.killCount;

            // HUD(화면)도 즉시 갱신해줘야 함
            if (PlayerHUD.Instance != null)
            {
                PlayerHUD.Instance.UpdateLevel(level);
                PlayerHUD.Instance.UpdateExp(exp, maxExp);
                // 킬수 갱신 함수가 따로 없다면 아래처럼, 있다면 해당 함수 호출
                // PlayerHUD.Instance.UpdateKill(killCount); 
            }

            Debug.Log("데이터 로드 완료! 레벨: " + level);
        }
        else
        {
            // ★ 새로하기(New Game)일 때만 처음 레벨업(무기 선택) 창을 띄움
            StartCoroutine(ShowInitialLevelUp());
        }
    }

    private void Update()
    {
        if (IsGameRunning && CurrentState == GameState.Playing)
        {
            GameTime += Time.deltaTime;
            OnGameTimeUpdated?.Invoke(GameTime);
        }
    }

    // 게임 시작
    public void StartGame()
    {
        ChangeState(GameState.Playing);
        IsGameRunning = true;
        GameTime = 0f;
    }

    // 초기 레벨업 UI 표시 코루틴
    private IEnumerator ShowInitialLevelUp()
    {
        // 1초 대기하여 모든 컴포넌트가 초기화될 때까지 기다림
        yield return new WaitForSeconds(3f);

        // LevelUpManager의 ShowLevelUp 메서드 호출
        if (levelUpManager != null)
        {
            levelUpManager.ShowLevelUp();
            Debug.Log("초기 레벨업 팝업을 표시합니다.");
        }
    }

    // 게임 일시정지
    public void PauseGame()
    {
        ChangeState(GameState.Paused);
        Time.timeScale = 0f;
    }

    // 게임 재개
    public void ResumeGame()
    {
        ChangeState(GameState.Playing);
        Time.timeScale = 1f;
    }

    // 게임 오버
    public void GameOver()
    {
        ChangeState(GameState.GameOver);
        IsGameRunning = false;
        Time.timeScale = 0f;
    }

    // 게임 승리
    public void Victory()
    {
        ChangeState(GameState.Victory);
        IsGameRunning = false;
        Time.timeScale = 0f;
    }

    // 상태 변경
    private void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }
}