using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 게임 상태(Playing, Paused, GameOver, Victory)를 관리하고 게임 시간, 레벨업 UI, 통계 보고서를 제어하는 매니저
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    #region Serialized Fields
    [Header("Settings")]
    [Tooltip("게임 시작 후 초기 레벨업 UI 표시까지의 대기 시간")]
    [SerializeField] private float initialLevelUpDelay = 3f;

    [Tooltip("게임 승리까지 필요한 시간 (분 단위, 0이면 무제한)")]
    [SerializeField] private float victoryTimeMinutes = 15f;

    [Header("References")]
    [Tooltip("레벨업 매니저 참조 (자동 할당됨)")]
    [SerializeField] private LevelUpManager levelUpManager;

    [Header("Drop Exp System")]
    [Tooltip("드롭 경험치 티어 관리용 데이터 매니저")]
    [SerializeField] private DataManager dataManager;
    #endregion

    #region Public Fields
    // 플레이어 참조
    public Transform player;

    // 현재 드롭 경험치 티어 인덱스
    public int currentTierIndex = 0;
    #endregion

    #region Enums
    /// <summary>
    /// 게임 상태 열거형
    /// </summary>
    public enum GameState
    {
        Playing,    // 플레이 중
        Paused,     // 일시정지
        GameOver,   // 게임 오버
        Victory      // 승리
    }
    #endregion

    #region Properties
    /// <summary>
    /// 현재 게임 상태
    /// </summary>
    public GameState CurrentState { get; private set; }

    /// <summary>
    /// 게임 플레이 시간 (초 단위)
    /// </summary>
    public float GameTime { get; private set; }

    /// <summary>
    /// 게임이 실행 중인지 여부
    /// </summary>
    public bool IsGameRunning { get; private set; }
    #endregion

    #region Events
    /// <summary>
    /// 게임 상태 변경 이벤트
    /// [v2 수정] OnGameStateChanged → GameStateChanged (이벤트 네이밍 규칙 준수)
    /// </summary>
    public event Action<GameState> GameStateChanged;

    /// <summary>
    /// 게임 시간 업데이트 이벤트
    /// </summary>
    public event Action<float> OnGameTimeUpdated;
    #endregion

    #region Unity Lifecycle
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
        // [v3 수정] UIManager 없을 때를 대비해 게임 자동 시작
        // 타이틀 화면이 구현되면 제거 가능
        StartGame();

        // 레벨업 매니저 자동 찾기 (인스펙터 미할당 시)
        if (levelUpManager == null)
        {
            levelUpManager = FindObjectOfType<LevelUpManager>();
        }

        // 데이터 매니저 자동 찾기 (인스펙터 미할당 시)
        if (dataManager == null)
        {
            dataManager = FindObjectOfType<DataManager>();
        }
    }

    private void Update()
    {
        // 게임 플레이 중일 때만 시간 증가
        if (IsGameRunning && CurrentState == GameState.Playing)
        {
            GameTime += Time.deltaTime;
            OnGameTimeUpdated?.Invoke(GameTime);

            // 승리 조건 체크 (0이면 무제한)
            if (victoryTimeMinutes > 0f && GameTime >= victoryTimeMinutes * 60f)
            {
                Victory();
            }
        }
    }
    #endregion

    #region Public API - 게임 흐름 제어
    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        ChangeState(GameState.Playing);
        IsGameRunning = true;
        GameTime = 0f;

        // 명확하게 시간 스케일 설정
        Time.timeScale = 1f;

        // 초기 레벨업 UI 표시 코루틴 시작
        StartCoroutine(ShowInitialLevelUp());

        Debug.Log("[GameManager] 게임 시작");
    }

    /// <summary>
    /// 게임 일시정지
    /// </summary>
    public void PauseGame()
    {
        ChangeState(GameState.Paused);
        Time.timeScale = 0f;

        Debug.Log("[GameManager] 게임 일시정지");
    }

    /// <summary>
    /// 게임 재개
    /// </summary>
    public void ResumeGame()
    {
        ChangeState(GameState.Playing);
        Time.timeScale = 1f;

        Debug.Log("[GameManager] 게임 재개");
    }

    /// <summary>
    /// 게임 오버
    /// </summary>
    public void GameOver()
    {
        ChangeState(GameState.GameOver);
        IsGameRunning = false;
        Time.timeScale = 0f;

        Debug.Log("[GameManager] 게임 오버!");

        // [v2 추가] 통계 시스템 연동
        ShowStatistics();
    }

    /// <summary>
    /// [v2 추가] 통계 보고서 표시
    /// </summary>
    private void ShowStatistics()
    {
        if (StatisticsManager.Instance == null)
        {
            Debug.LogWarning("[GameManager] StatisticsManager를 찾을 수 없습니다.");
            return;
        }

        // 통계 보고서 요청
        List<SkillSessionData> report = StatisticsManager.Instance.GetReport(GameTime);

        // 콘솔로그 출력 (디버깅용)
        Debug.Log($"[Statistics] ========== 게임 종료 (플레이 시간: {GameTime:F1}초) ==========");
        Debug.Log($"[Statistics] 총 피해량: {StatisticsManager.Instance.GetTotalDamage():F0}");
        Debug.Log($"[Statistics] 총 적중 횟수: {StatisticsManager.Instance.GetTotalHits()}");

        foreach (var skillData in report)
        {
            Debug.Log($"[Statistics] {skillData.skillName}: Total {skillData.totalDamage:F0}, DPS: {skillData.dps:F1}, Hits: {skillData.hitCount}");
        }

        // TODO: UI에 전달 (예시)
        // UIManager.Instance.ShowResultScreen(report);
    }

    /// <summary>
    /// 게임 승리
    /// </summary>
    public void Victory()
    {
        ChangeState(GameState.Victory);
        IsGameRunning = false;
        Time.timeScale = 0f;

        // PlayerHUD에 승리 UI 표시 요청
        if (PlayerHUD.Instance != null)
        {
            PlayerHUD.Instance.ShowVictoryUI();
        }

        Debug.Log("[GameManager] 게임 승리!");
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 게임 상태 변경 및 이벤트 발생
    /// </summary>
    private void ChangeState(GameState newState)
    {
        CurrentState = newState;

        // [v2 수정] 이벤트 이름 변경
        GameStateChanged?.Invoke(newState);
    }

    /// <summary>
    /// 초기 레벨업 UI 표시 코루틴
    /// </summary>
    private IEnumerator ShowInitialLevelUp()
    {
        // [v2 수정] 매직 넘버 제거 - SerializeField 변수 사용
        yield return new WaitForSeconds(initialLevelUpDelay);

        // LevelUpManager의 ShowLevelUp 메서드 호출
        if (levelUpManager != null)
        {
            levelUpManager.ShowLevelUp();
        }
        else
        {
            Debug.LogWarning("[GameManager] LevelUpManager를 찾을 수 없습니다.");
        }
    }
    #endregion

    #region Public API - 설정 관련
    /// <summary>
    /// 레벨업 매니저 설정 (런타임에 참조 설정용)
    /// </summary>
    public void SetLevelUpManager(LevelUpManager manager)
    {
        levelUpManager = manager;
    }

    /// <summary>
    /// 데이터 매니저 설정 (런타임에 참조 설정용)
    /// </summary>
    public void SetDataManager(DataManager manager)
    {
        dataManager = manager;
    }

    /// <summary>
    /// 초기 레벨업 대기 시간 설정 (기획자 변경용)
    /// </summary>
    public void SetInitialLevelUpDelay(float delay)
    {
        initialLevelUpDelay = Mathf.Max(0f, delay);
    }
    #endregion
}
