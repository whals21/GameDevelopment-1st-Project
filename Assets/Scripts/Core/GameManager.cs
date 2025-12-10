using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static GameManager Instance { get; private set; }

     // 플레이어 참조 
    public Transform player;   

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
         if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
        StartGame();
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