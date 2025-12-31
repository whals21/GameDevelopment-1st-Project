using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 게임 결과용 스킬 세션 통계 데이터 (총 피해량, DPS, 적중 횟수)
/// </summary>
[System.Serializable]
public class SkillSessionData
{
    public string skillName;      // 스킬 이름
    public string iconPath;       // 아이콘 경로 (스프라이트 이름)
    public float totalDamage;     // 총 피해량
    public float dps;             // 초당 데미지 (DPS)
    public int hitCount;          // 적중 횟수
    public SkillType skillType;   // 스킬 타입

    /// <summary>
    /// 디버그용 문자열 반환
    /// </summary>
    public override string ToString()
    {
        return $"[{skillName}] Total: {totalDamage:F0}, DPS: {dps:F1}, Hits: {hitCount}";
    }
}

/// <summary>
/// 인스펙터 표시용 직렬화 가능한 스킬 통계
/// </summary>
[System.Serializable]
public class SkillStatDisplay
{
    public string skillName;
    public float totalDamage;
    public int hitCount;
    public float dps;
}

/// <summary>
/// 스킬별 총 피해량, DPS, 적중 횟수를 기록하고 게임 종료 시 통계 보고서를 생성하는 매니저
/// </summary>
public class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance { get; private set; }

    #region Inspector Debug Fields (실시간 확인용)
    [Header("=== 실시간 통계 (인스펙터용) ===")]
    [SerializeField] private float _totalDamage;
    [SerializeField] private int _totalHits;
    [SerializeField] private float _currentDPS;
    [SerializeField] private float _gameTime;

    [Header("스킬별 통계")]
    [SerializeField] private List<SkillStatDisplay> _skillStats = new List<SkillStatDisplay>();

    // 실시간 업데이트 간격 (성능 고려하여 0.5초마다 갱신)
    [SerializeField] private float _debugUpdateInterval = 0.5f;
    private float _debugUpdateTimer;
    #endregion

    #region Private Fields
    // 스킬별 총 피해량 (Key: 스킬 이름, Value: 총 피해량)
    // 같은 Projectile 타입이라도 스킬별로 개별 추적 (화염병, RPG, Brick 등)
    private Dictionary<string, float> _skillTotalDamage = new Dictionary<string, float>();

    // 스킬별 적중 횟수
    private Dictionary<string, int> _skillHitCount = new Dictionary<string, int>();
    #endregion

    #region Initialization
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // 게임 시작 시 통계 리셋
        ResetStatistics();
    }

    /// <summary>
    /// 실시간 인스펙터 업데이트
    /// </summary>
    private void Update()
    {
        _debugUpdateTimer += Time.deltaTime;

        // 일정 간격으로 업데이트 (성능 최적화)
        if (_debugUpdateTimer >= _debugUpdateInterval)
        {
            _debugUpdateTimer = 0f;
            UpdateInspectorFields();
        }
    }

    /// <summary>
    /// 인스펙터 필드 실시간 업데이트
    /// </summary>
    private void UpdateInspectorFields()
    {
        // 게임 시간 (GameManager에서 가져오기)
        if (GameManager.Instance != null)
        {
            _gameTime = GameManager.Instance.GameTime;
        }

        // 총합 계산
        _totalDamage = GetTotalDamage();
        _totalHits = GetTotalHits();

        // 전체 DPS 계산
        if (_gameTime > 0f)
        {
            _currentDPS = _totalDamage / _gameTime;
        }

        // 스킬별 통계 업데이트
        _skillStats.Clear();
        foreach (var kvp in _skillTotalDamage)
        {
            _skillHitCount.TryGetValue(kvp.Key, out int hits);

            float skillDPS = 0f;
            if (_gameTime > 0f)
            {
                skillDPS = kvp.Value / _gameTime;
            }

            _skillStats.Add(new SkillStatDisplay
            {
                skillName = kvp.Key,  // 이미 스킬 이름(string)임
                totalDamage = kvp.Value,
                hitCount = hits,
                dps = skillDPS
            });
        }

        // 총 피해량 내림차순 정렬
        _skillStats.Sort((a, b) => b.totalDamage.CompareTo(a.totalDamage));
    }
    #endregion

    #region Public API - Recording
    /// <summary>
    /// 스킬 피해량 기록 (적이 데미지를 받을 때 호출)
    /// </summary>
    /// <param name="skillName">스킬 이름 (예: "화염병", "번개", "보호막")</param>
    /// <param name="damage">입힌 데미지</param>
    public void RecordDamage(string skillName, float damage)
    {
        // 모든 스킬(Aura 포함) 기록
        // 같은 Projectile 타입이라도 스킬 이름으로 개별 추적

        // 총 피해량 누적
        if (_skillTotalDamage.ContainsKey(skillName))
        {
            _skillTotalDamage[skillName] += damage;
            _skillHitCount[skillName]++;
        }
        else
        {
            _skillTotalDamage.Add(skillName, damage);
            _skillHitCount.Add(skillName, 1);
        }
    }

    /// <summary>
    /// 통계 리셋 (새 게임 시작 시 호출)
    /// </summary>
    public void ResetStatistics()
    {
        _skillTotalDamage.Clear();
        _skillHitCount.Clear();

        // 인스펙터 필드도 리셋
        _totalDamage = 0f;
        _totalHits = 0;
        _currentDPS = 0f;
        _gameTime = 0f;
        _skillStats.Clear();
    }
    #endregion

    #region Public API - Report
    /// <summary>
    /// 최종 통계 보고서 생성 (게임 종료 시 호출)
    /// </summary>
    /// <param name="gameTime">총 게임 플레이 시간 (초)</param>
    /// <returns>통계 리스트</returns>
    public List<SkillSessionData> GetReport(float gameTime)
    {
        List<SkillSessionData> report = new List<SkillSessionData>();

        // 각 기록된 스킬에 대한 보고서 생성
        foreach (var kvp in _skillTotalDamage)
        {
            string skillName = kvp.Key;  // 이미 스킬 이름
            float totalDamage = kvp.Value;
            _skillHitCount.TryGetValue(skillName, out int hitCount);

            // DPS 계산 (초당 데미지)
            float dps = 0f;
            if (gameTime > 0f)
            {
                dps = totalDamage / gameTime;
            }

            // 스킬 이름으로 이미 구분되어 있으므로 바로 추가
            report.Add(new SkillSessionData
            {
                skillName = skillName,
                iconPath = "",
                totalDamage = totalDamage,
                dps = dps,
                hitCount = hitCount,
                skillType = SkillType.Projectile  // 기본값 (필요시 변경)
            });
        }

        // 총 피해량 기준 정렬 (내림차순)
        report.Sort((a, b) => b.totalDamage.CompareTo(a.totalDamage));

        return report;
    }

    /// <summary>
    /// 전체 총 피해량 반환 (모든 스킬 합계)
    /// </summary>
    public float GetTotalDamage()
    {
        float total = 0f;
        foreach (var damage in _skillTotalDamage.Values)
        {
            total += damage;
        }
        return total;
    }

    /// <summary>
    /// 전체 적중 횟수 반환
    /// </summary>
    public int GetTotalHits()
    {
        int total = 0;
        foreach (var count in _skillHitCount.Values)
        {
            total += count;
        }
        return total;
    }
    #endregion

    #region Debug
    /// <summary>
    /// 현재 통계 상태를 콘솔에 출력 (디버깅용)
    /// </summary>
    public void DebugPrintStatistics()
    {
        Debug.Log($"[StatisticsManager] ===== Statistics Report =====");
        Debug.Log($"[StatisticsManager] Total Damage: {GetTotalDamage():F0}");
        Debug.Log($"[StatisticsManager] Total Hits: {GetTotalHits()}");

        foreach (var kvp in _skillTotalDamage)
        {
            _skillHitCount.TryGetValue(kvp.Key, out int hits);
            Debug.Log($"[StatisticsManager] {kvp.Key}: Damage={kvp.Value:F0}, Hits={hits}");
        }
    }
    #endregion
}
