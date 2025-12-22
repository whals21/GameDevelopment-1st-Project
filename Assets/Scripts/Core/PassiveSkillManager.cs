using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 패시브 스킬 관리 시스템
/// 스킬 획득, 레벨 관리, 효과 적용을 담당
/// </summary>
public class PassiveSkillManager : MonoBehaviour
{
    public static PassiveSkillManager Instance { get; private set; }

    [Header("패시브 스킬 데이터")]
    [SerializeField] private PassiveSkillData[] allPassiveSkills;

    [Header("설정")]
    [SerializeField] private int skillOfferCount = 3;        // 스킬 제시 개수
    [SerializeField] private float baseOfferChance = 0.3f;  // 기본 스킬 제시 확률
    [SerializeField] private float offerChanceIncreasePerLevel = 0.05f; // 레벨당 제시 확률 증가

    // 현재 보유한 패시브 스킬들 (타입, 레벨)
    private Dictionary<PassiveSkillType, int> ownedPassiveSkills = new Dictionary<PassiveSkillType, int>();

    // 캐싱된 스킬 데이터 (타입, 데이터)
    private Dictionary<PassiveSkillType, PassiveSkillData> skillDataCache = new Dictionary<PassiveSkillType, PassiveSkillData>();

    // 이벤트 정의
    public System.Action<PassiveSkillData, int> OnSkillLevelUp;      // 스킬 레벨 업 이벤트
    public System.Action<PassiveSkillData, int> OnSkillAcquired;     // 새 스킬 획득 이벤트
    public System.Action OnPassiveSkillsUpdated;                     // 패시브 스킬 업데이트 이벤트

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSkillCache();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 초기 패시브 스킬 적용
        ApplyAllPassiveEffects();

        // 인스펙터에 테스트 스킬이 설정되어 있으면 자동 적용 (지연 실행)
        Invoke(nameof(CheckAndAutoApplyTestSkills), 0.5f);
    }

    /// <summary>
    /// 인스펙터에 테스트 스킬이 설정되어 있으면 자동 적용
    /// </summary>
    private void CheckAndAutoApplyTestSkills()
    {
        if (SkillManager.Instance != null && SkillManager.Instance.testPassiveSkills != null)
        {
            bool hasTestSkills = false;
            foreach (var skill in SkillManager.Instance.testPassiveSkills)
            {
                if (skill != null)
                {
                    hasTestSkills = true;
                    break;
                }
            }

            if (hasTestSkills && ownedPassiveSkills.Count == 0)
            {
                Debug.Log("[PassiveSkillManager] 테스트 패시브 스킬이 설정되어 있어 자동으로 등록합니다...");
                SkillManager.Instance.RegisterTestPassiveSkills();
            }
        }
    }

    /// <summary>
    /// 스킬 데이터 캐시 초기화
    /// </summary>
    private void InitializeSkillCache()
    {
        skillDataCache.Clear();

        if (allPassiveSkills != null)
        {
            foreach (var skillData in allPassiveSkills)
            {
                if (skillData != null && !skillDataCache.ContainsKey(skillData.passiveType))
                {
                    skillDataCache.Add(skillData.passiveType, skillData);
                }
            }
        }

        Debug.Log($"[PassiveSkillManager] 스킬 데이터 캐시 초기화 완료: {skillDataCache.Count}개 스킬");
    }

    /// <summary>
    /// 스킬 획득 시도
    /// </summary>
    /// <param name="skillType">획득하려는 스킬 타입</param>
    /// <returns>획득 성공 여부</returns>
    public bool TryAcquireSkill(PassiveSkillType skillType)
    {
        // 스킬 데이터 확인
        if (!skillDataCache.TryGetValue(skillType, out PassiveSkillData skillData))
        {
            Debug.LogError($"[PassiveSkillManager] {skillType} 스킬 데이터를 찾을 수 없음");
            return false;
        }

        // 이미 보유한 스킬인지 확인
        if (ownedPassiveSkills.ContainsKey(skillType))
        {
            // 이미 보유한 스킬이면 레벨 업 가능한지 확인
            if (ownedPassiveSkills[skillType] >= skillData.maxLevel)
            {
                Debug.Log($"[PassiveSkillManager] {skillType} 스킬은 이미 최대 레벨 ({skillData.maxLevel})입니다.");
                return false;
            }

            // 레벨 업
            int newLevel = ++ownedPassiveSkills[skillType];
            Debug.Log($"[PassiveSkillManager] {skillData.skillName} 스킬이 레벨 {newLevel}(으)로 상승했습니다.");

            OnSkillLevelUp?.Invoke(skillData, newLevel);
            ApplyAllPassiveEffects();
            return true;
        }
        else
        {
            // 새로운 스킬 획득 (레벨 1로 시작)
            ownedPassiveSkills.Add(skillType, 1);
            Debug.Log($"[PassiveSkillManager] {skillData.skillName} 스킬을 새로 획득했습니다 (레벨 1).");

            OnSkillAcquired?.Invoke(skillData, 1);
            ApplyAllPassiveEffects();
            return true;
        }
    }

    /// <summary>
    /// 랜덤 패시브 스킬 제시 목록 생성
    /// </summary>
    /// <param name="playerLevel">플레이어 레벨</param>
    /// <returns>제시할 스킬 목록</returns>
    public List<PassiveSkillData> GetRandomSkillOffers(int playerLevel)
    {
        List<PassiveSkillData> offers = new List<PassiveSkillData>();
        List<PassiveSkillData> availableSkills = new List<PassiveSkillData>(skillDataCache.Values);

        // 우선순위 정렬 (높은 순위 먼저)
        availableSkills = availableSkills.OrderByDescending(s => s.priority).ToList();

        // 스킬 제시 개수만큼 반복
        for (int i = 0; i < skillOfferCount && availableSkills.Count > 0; i++)
        {
            PassiveSkillData selectedSkill = null;

            // 1. 보유한 스킬 중 최대 레벨이 아닌 스킬 우선 선택
            var upgradeableSkills = availableSkills.Where(s =>
                ownedPassiveSkills.ContainsKey(s.passiveType) &&
                ownedPassiveSkills[s.passiveType] < s.maxLevel).ToList();

            if (upgradeableSkills.Count > 0)
            {
                selectedSkill = upgradeableSkills[Random.Range(0, upgradeableSkills.Count)];
            }
            else
            {
                // 2. 새로운 스킬 선택
                var newSkills = availableSkills.Where(s => !ownedPassiveSkills.ContainsKey(s.passiveType)).ToList();

                if (newSkills.Count > 0)
                {
                    // 확률 가중치 적용 (우선순위가 높을수록 선택 확률 증가)
                    float totalWeight = newSkills.Sum(s => s.priority + 1);
                    float randomWeight = Random.Range(0f, totalWeight);
                    float currentWeight = 0f;

                    foreach (var skill in newSkills)
                    {
                        currentWeight += skill.priority + 1;
                        if (currentWeight >= randomWeight)
                        {
                            selectedSkill = skill;
                            break;
                        }
                    }
                }
            }

            // 선택된 스킬을 제시 목록에 추가하고 후보에서 제거
            if (selectedSkill != null)
            {
                offers.Add(selectedSkill);
                availableSkills.Remove(selectedSkill);
            }
            else
            {
                // 선택할 스킬이 없으면 종료
                break;
            }
        }

        return offers;
    }

    /// <summary>
    /// 모든 패시브 효과 적용
    /// </summary>
    private void ApplyAllPassiveEffects()
    {
        Debug.Log($"[PassiveSkillManager] ApplyAllPassiveEffects 시작 - 보유 스킬 수: {ownedPassiveSkills.Count}");

        // PlayerStats 컴포넌트 찾기
        var playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogWarning("[PassiveSkillManager] PlayerStats 컴포넌트를 찾을 수 없습니다.");
        }
        else
        {
            Debug.Log("[PassiveSkillManager] PlayerStats 찾음");
        }

        // PlayerController 찾기 (미리 찾아두기)
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("[PassiveSkillManager] PlayerController를 찾을 수 없습니다.");
        }
        else
        {
            Debug.Log("[PassiveSkillManager] PlayerController 찾음");
        }

        // 각 패시브 효과 적용
        int successCount = 0;
        foreach (var kvp in ownedPassiveSkills)
        {
            PassiveSkillType skillType = kvp.Key;
            int level = kvp.Value;

            Debug.Log($"[PassiveSkillManager] 스킬 적용 시도: {skillType} Lv.{level}");

            if (skillDataCache.TryGetValue(skillType, out PassiveSkillData skillData))
            {
                float effectValue = skillData.GetEffectValue(level);
                Debug.Log($"[PassiveSkillManager] {skillType} 효과값: {effectValue}");

                if (ApplyPassiveEffect(skillType, effectValue, playerStats, playerController))
                {
                    successCount++;
                    Debug.Log($"[PassiveSkillManager] {skillType} 효과 적용 성공");
                }
                else
                {
                    Debug.LogError($"[PassiveSkillManager] {skillType} 효과 적용 실패");
                }
            }
            else
            {
                Debug.LogError($"[PassiveSkillManager] {skillType} 스킬 데이터를 캐시에서 찾을 수 없음");
            }
        }

        Debug.Log($"[PassiveSkillManager] ApplyAllPassiveEffects 완료 - 성공: {successCount}/{ownedPassiveSkills.Count}");
        OnPassiveSkillsUpdated?.Invoke();
    }

    /// <summary>
    /// 특정 패시브 효과 적용
    /// </summary>
    /// <param name="skillType">스킬 타입</param>
    /// <param name="value">효과값</param>
    /// <param name="playerStats">플레이어 스탯</param>
    /// <param name="playerController">플레이어 컨트롤러</param>
    /// <returns>적용 성공 여부</returns>
    private bool ApplyPassiveEffect(PassiveSkillType skillType, float value, PlayerStats playerStats, PlayerController playerController = null)
    {
        try
        {
            switch (skillType)
            {
                case PassiveSkillType.ExperienceBonus:
                    Debug.Log($"[PassiveSkillManager] 경험치 보너스 {value*100}% 적용 (미구현)");
                    return true; // 경험치 보너스는 별도 관리 (추가 구현 필요)

                case PassiveSkillType.MagnetRange:
                    Debug.Log($"[PassiveSkillManager] 마그넷 범위 {value}m 증가 적용 (미구현)");
                    return true; // 마그넷 범위 증가 (추가 구현 필요)

                case PassiveSkillType.DamageBoost:
                    if (SkillManager.Instance != null)
                    {
                        SkillManager.Instance.SetDamageMultiplier(1f + value);
                        Debug.Log($"[PassiveSkillManager] 공격력 배수 {1f + value:F2}로 설정 완료");
                        return true;
                    }
                    else
                    {
                        Debug.LogError("[PassiveSkillManager] SkillManager.Instance가 null");
                        return false;
                    }

                case PassiveSkillType.MaxHealth:
                    if (playerStats != null)
                    {
                        playerStats.AddMaxHp(value);
                        Debug.Log($"[PassiveSkillManager] 최대 체력 {value} 증가 완료");
                        return true;
                    }
                    else
                    {
                        Debug.LogError("[PassiveSkillManager] PlayerStats가 null");
                        return false;
                    }

                case PassiveSkillType.HealthRegen:
                    Debug.Log($"[PassiveSkillManager] 체력 재생 {value}/초 적용 (미구현)");
                    return true; // 체력 재생 (추가 구현 필요)

                case PassiveSkillType.MovementSpeed:
                    // 미리 찾은 PlayerController 사용, 없으면 다시 찾기
                    if (playerController == null)
                    {
                        playerController = FindObjectOfType<PlayerController>();
                    }

                    if (playerController != null)
                    {
                        // 리플렉션으로 SetMovementSpeedMultiplier 메서드 확인
                        var method = playerController.GetType().GetMethod("SetMovementSpeedMultiplier");
                        if (method != null)
                        {
                            method.Invoke(playerController, new object[] { 1f + value });
                            Debug.Log($"[PassiveSkillManager] 이동속도 배수 {1f + value:F2}로 설정 완료");
                            return true;
                        }
                        else
                        {
                            Debug.LogError("[PassiveSkillManager] PlayerController에 SetMovementSpeedMultiplier 메서드가 없음");
                            return false;
                        }
                    }
                    else
                    {
                        Debug.LogError("[PassiveSkillManager] PlayerController를 찾을 수 없음");
                        return false;
                    }

                case PassiveSkillType.CooldownReduction:
                    if (SkillManager.Instance != null)
                    {
                        SkillManager.Instance.SetCooldownMultiplier(1f - value);
                        Debug.Log($"[PassiveSkillManager] 쿨다운 배수 {1f - value:F2}로 설정 완료");
                        return true;
                    }
                    else
                    {
                        Debug.LogError("[PassiveSkillManager] SkillManager.Instance가 null");
                        return false;
                    }

                case PassiveSkillType.DropRateBonus:
                    Debug.Log($"[PassiveSkillManager] 드롭률 보너스 {value*100}% 적용 (미구현)");
                    return true; // 드롭률 증가 (추가 구현 필요)

                default:
                    Debug.LogWarning($"[PassiveSkillManager] 처리되지 않은 패시브 스킬 타입: {skillType}");
                    return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PassiveSkillManager] {skillType} 적용 중 예외 발생: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 현재 보유한 스킬의 레벨 반환
    /// </summary>
    /// <param name="skillType">스킬 타입</param>
    /// <returns>스킬 레벨 (보유하지 않았으면 0)</returns>
    public int GetSkillLevel(PassiveSkillType skillType)
    {
        return ownedPassiveSkills.TryGetValue(skillType, out int level) ? level : 0;
    }

    /// <summary>
    /// 특정 타입의 스킬을 보유하고 있는지 확인
    /// </summary>
    /// <param name="skillType">스킬 타입</param>
    /// <returns>보유하고 있으면 true</returns>
    public bool HasSkill(PassiveSkillType skillType)
    {
        return ownedPassiveSkills.ContainsKey(skillType);
    }

    /// <summary>
    /// 현재 보유한 모든 패시브 스킬 정보 반환
    /// </summary>
    /// <returns>(스킬 타입, 레벨) 딕셔너리</returns>
    public Dictionary<PassiveSkillType, int> GetAllOwnedSkills()
    {
        return new Dictionary<PassiveSkillType, int>(ownedPassiveSkills);
    }

    /// <summary>
    /// 스킬 획득 확률 계산
    /// </summary>
    /// <param name="playerLevel">플레이어 레벨</param>
    /// <returns>스킬 제시 확률</returns>
    public float GetSkillOfferChance(int playerLevel)
    {
        return Mathf.Clamp01(baseOfferChance + (playerLevel - 1) * offerChanceIncreasePerLevel);
    }

    /// <summary>
    /// 모든 패시브 스킬 초기화 (디버그용)
    /// </summary>
    [ContextMenu("모든 패시브 스킬 초기화")]
    public void ResetAllPassiveSkills()
    {
        ownedPassiveSkills.Clear();
        ApplyAllPassiveEffects();
        Debug.Log("[PassiveSkillManager] 모든 패시브 스킬이 초기화되었습니다.");
    }

    /// <summary>
    /// 모든 패시브 스킬 최대 레벨로 설정 (테스트용)
    /// </summary>
    [ContextMenu("모든 패시브 스킬 최대 레벨 설정")]
    public void MaxAllPassiveSkills()
    {
        foreach (var kvp in skillDataCache)
        {
            ownedPassiveSkills[kvp.Key] = kvp.Value.maxLevel;
        }
        ApplyAllPassiveEffects();
        Debug.Log("[PassiveSkillManager] 모든 패시브 스킬이 최대 레벨로 설정되었습니다.");
    }

    /// <summary>
    /// 인스펙터에서 설정한 테스트 패시브 스킬 자동 적용
    /// </summary>
    [ContextMenu("인스펙터 설정 테스트 스킬 자동 적용")]
    public void AutoApplyInspectorTestSkills()
    {
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.RegisterTestPassiveSkills();
        }
        else
        {
            Debug.LogError("[PassiveSkillManager] SkillManager.Instance를 찾을 수 없습니다.");
        }
    }

    #region GUI Display - Optimized
    private bool showDebugGUI = true;
    private Rect guiRect = new Rect(10, 10, 280, 350);
    private Vector2 dragPosition = Vector2.zero;
    private bool isDragging = false;

    // 캐싱용으로 성능 최적화
    private PlayerStats cachedPlayerStats;
    private float lastGuiUpdateTime = 0f;
    private const float GUI_UPDATE_INTERVAL = 0.1f; // 100ms마다 업데이트
    private string cachedSkillInfo = "";
    private string cachedBuffInfo = "";

    private void OnGUI()
    {
        if (!showDebugGUI) return;

        // GUI 업데이트 빈도 제어
        if (Time.realtimeSinceStartup - lastGuiUpdateTime > GUI_UPDATE_INTERVAL)
        {
            UpdateCachedInfo();
            lastGuiUpdateTime = Time.realtimeSinceStartup;
        }

        // 간단한 드래그 처리
        if (isDragging && Event.current.type == EventType.MouseUp)
        {
            isDragging = false;
        }

        // 드래그 중일 때만 위치 계산
        if (isDragging)
        {
            dragPosition = GUIUtility.ScreenToGUIPoint(Input.mousePosition) - (Vector2)guiRect.position;
        }

        // 창 렌더링 최적화
        Rect windowRect = new Rect(guiRect.position + dragPosition, guiRect.size);

        // 스타일 최적화 - 간단한 계산만 수행
        windowRect = GUI.Window(0, windowRect, DoGUIOptimized, "패시브 스킬 상태");
    }

    private void UpdateCachedInfo()
    {
        // 스킬 정보 업데이트
        if (ownedPassiveSkills.Count == 0)
        {
            cachedSkillInfo = "보유한 패시브 스킬이 없습니다.\n'테스트 패시브 스킬 등록'을 실행하세요.";
        }
        else
        {
            var skillText = new System.Text.StringBuilder();
            foreach (var kvp in ownedPassiveSkills)
            {
                if (skillDataCache.TryGetValue(kvp.Key, out PassiveSkillData skillData))
                {
                    float effectValue = skillData.GetEffectValue(kvp.Value);
                    string effectText = GetEffectDisplayText(kvp.Key, effectValue);
                    skillText.AppendLine($"• {skillData.skillName} Lv.{kvp.Value} - {effectText}");
                }
            }
            cachedSkillInfo = skillText.ToString();
        }

        // 버프 정보 업데이트
        var buffText = new System.Text.StringBuilder();

        if (SkillManager.Instance != null)
        {
            float damageMultiplier = SkillManager.Instance.GetGlobalDamageMultiplier();
            float cooldownMultiplier = SkillManager.Instance.GetGlobalCooldownMultiplier();

            buffText.AppendLine($"공격력: {damageMultiplier:F2}x ({(damageMultiplier - 1) * 100:+0;-0}%)");
            buffText.AppendLine($"쿨다운: {cooldownMultiplier:F2}x ({(cooldownMultiplier - 1) * 100:+0;-0}%)");
        }

        // PlayerStats 캐싱
        if (cachedPlayerStats == null)
        {
            cachedPlayerStats = FindObjectOfType<PlayerStats>();
        }

        if (cachedPlayerStats != null)
        {
            buffText.AppendLine($"최대 체력: {cachedPlayerStats.MaxHp:F0}");
        }

        cachedBuffInfo = buffText.ToString();
    }

    private void DoGUIOptimized(int windowID)
    {
        // 헤더 - 최소한한 GUILayout 사용
        GUI.Label(new Rect(10, 5, 250, 20), "패시브 스킬 상태 (드래그 가능)", GUI.skin.label);
        if (GUI.Button(new Rect(250, 5, 20, 20), "✕"))
        {
            showDebugGUI = false;
        }

        // 내용 영역
        Rect contentRect = new Rect(10, 30, 260, guiRect.height - 60);

        // 스킬 정보
        GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, 20), "=== 보유한 패시브 스킬 ===");
        GUI.Label(new Rect(contentRect.x, contentRect.y + 25, contentRect.width, 80), cachedSkillInfo);

        // 버프 정보
        GUI.Label(new Rect(contentRect.x, contentRect.y + 110, contentRect.width, 20), "=== 현재 버프 상태 ===");
        GUI.Label(new Rect(contentRect.x, contentRect.y + 135, contentRect.width, 60), cachedBuffInfo);

        // 버튼 - 단순한 GUI.Button 사용
        Rect buttonRect = new Rect(contentRect.x, contentRect.y + 200, 80, 25);
        if (GUI.Button(buttonRect, "패시브 스킬 효과 재적용"))
        {
            ApplyAllPassiveEffects();
        }

        buttonRect.y += 30;
        if (GUI.Button(buttonRect, "모든 패시브 스킬 초기화"))
        {
            ResetAllPassiveSkills();
        }

        buttonRect.y += 30;
        if (GUI.Button(buttonRect, "GUI 숨기기"))
        {
            showDebugGUI = false;
        }

        buttonRect.y += 30;
        if (GUI.Button(buttonRect, "창 이동 (↔)"))
        {
            isDragging = true;
        }

        // 창 드래그 기능 - 간단한 호출
        if (GUI.Button(new Rect(250, 5, 30, guiRect.height - 35), ""))
        {
            isDragging = true;
        }
    }

    private string GetEffectDisplayText(PassiveSkillType skillType, float effectValue)
    {
        switch (skillType)
        {
            case PassiveSkillType.ExperienceBonus:
                return $"경험치 +{effectValue * 100}%";
            case PassiveSkillType.MagnetRange:
                return $"자석 범위 +{effectValue}m";
            case PassiveSkillType.DamageBoost:
                return $"공격력 +{effectValue * 100}%";
            case PassiveSkillType.MaxHealth:
                return $"최대 체력 +{effectValue:F0}";
            case PassiveSkillType.HealthRegen:
                return $"체력 재생 +{effectValue:F1}/초";
            case PassiveSkillType.MovementSpeed:
                return $"이동속도 +{effectValue * 100}%";
            case PassiveSkillType.CooldownReduction:
                return $"쿨다운 -{effectValue * 100}%";
            case PassiveSkillType.DropRateBonus:
                return $"드롭률 +{effectValue * 100}%";
            default:
                return $"효과값: {effectValue}";
        }
    }

    /// <summary>
    /// 디버그 GUI 표시/숨김 토글
    /// </summary>
    [ContextMenu("디버그 GUI 토글")]
    public void ToggleDebugGUI()
    {
        showDebugGUI = !showDebugGUI;
        Debug.Log($"[PassiveSkillManager] 디버그 GUI {(showDebugGUI ? "표시" : "숨김")}");
    }
    #endregion
}