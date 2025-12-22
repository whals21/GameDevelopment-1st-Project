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

        // 게임 시작 시 한 번만 레벨업 UI 표시
        StartCoroutine(ShowInitialLevelUp());
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