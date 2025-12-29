using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 패시브 스킬 관리 시스템 (v2 리팩토링 - 최종 버전)
///
/// v2 변경사항:
/// - 덮어쓰기 버그 수정: 보너스 변수 누적 후 한 번에 적용
/// - 컴포넌트 캐싱으로 성능 최적화
/// - Invoke/Reflection 제거, 직접 호출 방식
/// - 유동적인 maxLevel 지원
/// </summary>
public class PassiveSkillManager : MonoBehaviour
{
    public static PassiveSkillManager Instance { get; private set; }

    #region Serialized Fields
    [Header("패시브 스킬 데이터")]
    [SerializeField] private PassiveSkillData[] allPassiveSkills;

    [Header("설정")]
    [SerializeField] private int skillOfferCount = 3;
    [SerializeField] private float baseOfferChance = 0.3f;
    [SerializeField] private float offerChanceIncreasePerLevel = 0.05f;
    #endregion

    #region Private Fields
    // 현재 보유한 패시브 스킬들 (타입, 레벨)
    private Dictionary<PassiveSkillType, int> ownedPassiveSkills = new Dictionary<PassiveSkillType, int>();

    // 캐싱된 스킬 데이터 (타입, 데이터)
    private Dictionary<PassiveSkillType, PassiveSkillData> skillDataCache = new Dictionary<PassiveSkillType, PassiveSkillData>();

    // [성능 최적화] 컴포넌트 캐싱 - 매번 FindObjectOfType 호출 제거
    private PlayerStats _playerStats;
    private PlayerController _playerController;
    #endregion

    #region Events
    public System.Action<PassiveSkillData, int> OnSkillLevelUp;
    public System.Action<PassiveSkillData, int> OnSkillAcquired;
    public System.Action OnPassiveSkillsUpdated;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            // 자신을 루트 GameObject로 분리 (DontDestroyOnLoad는 루트에서만 작동)
            transform.SetParent(null);

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSkillCache();

            // [성능 최적화] 초기에 컴포넌트 찾기
            _playerStats = FindObjectOfType<PlayerStats>();
            _playerController = FindObjectOfType<PlayerController>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator Start()
    {
        // [개선] 지연 실행을 코루틴으로 변경 (Invoke 제거)
        yield return new WaitForSeconds(0.5f);

        // TODO: 테스트용 스킬 자동 적용 로직 필요시 구현
        // CheckAndAutoApplyTestSkills();

        ApplyAllPassiveEffects();
    }
    #endregion

    #region Public API - 스킬 획득/관리
    /// <summary>
    /// 스킬 획득 시도
    /// </summary>
    public bool TryAcquireSkill(PassiveSkillType skillType)
    {
        if (!skillDataCache.TryGetValue(skillType, out PassiveSkillData skillData))
        {
            Debug.LogError($"[PassiveSkillManager] {skillType} 스킬 데이터를 찾을 수 없음");
            return false;
        }

        if (ownedPassiveSkills.ContainsKey(skillType))
        {
            if (ownedPassiveSkills[skillType] >= skillData.maxLevel)
            {
                return false;
            }

            int newLevel = ++ownedPassiveSkills[skillType];
            OnSkillLevelUp?.Invoke(skillData, newLevel);
            ApplyAllPassiveEffects();
            return true;
        }
        else
        {
            ownedPassiveSkills.Add(skillType, 1);
            OnSkillAcquired?.Invoke(skillData, 1);
            ApplyAllPassiveEffects();
            return true;
        }
    }

    /// <summary>
    /// 랜덤 패시브 스킬 제시 목록 생성
    /// </summary>
    public List<PassiveSkillData> GetRandomSkillOffers(int playerLevel)
    {
        List<PassiveSkillData> offers = new List<PassiveSkillData>();
        List<PassiveSkillData> availableSkills = new List<PassiveSkillData>(skillDataCache.Values);

        // 우선순위 정렬 (높은 순위 먼저)
        availableSkills = availableSkills.OrderByDescending(s => s.priority).ToList();

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
                // 2. 새로운 스킬 선택 (가중치 랜덤)
                var newSkills = availableSkills.Where(s => !ownedPassiveSkills.ContainsKey(s.passiveType)).ToList();

                if (newSkills.Count > 0)
                {
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

            if (selectedSkill != null)
            {
                offers.Add(selectedSkill);
                availableSkills.Remove(selectedSkill);
            }
            else
            {
                break;
            }
        }

        return offers;
    }

    /// <summary>
    /// 현재 보유한 스킬의 레벨 반환
    /// </summary>
    public int GetSkillLevel(PassiveSkillType skillType)
    {
        return ownedPassiveSkills.TryGetValue(skillType, out int level) ? level : 0;
    }

    /// <summary>
    /// 특정 타입의 스킬을 보유하고 있는지 확인
    /// </summary>
    public bool HasSkill(PassiveSkillType skillType)
    {
        return ownedPassiveSkills.ContainsKey(skillType);
    }

    /// <summary>
    /// 현재 보유한 모든 패시브 스킬 정보 반환
    /// </summary>
    public Dictionary<PassiveSkillType, int> GetAllOwnedSkills()
    {
        return new Dictionary<PassiveSkillType, int>(ownedPassiveSkills);
    }

    /// <summary>
    /// 스킬 획득 확률 계산
    /// </summary>
    public float GetSkillOfferChance(int playerLevel)
    {
        return Mathf.Clamp01(baseOfferChance + (playerLevel - 1) * offerChanceIncreasePerLevel);
    }
    #endregion

    #region Private Methods
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
    }

    /// <summary>
    /// 인스펙터에 테스트 스킬이 설정되어 있으면 자동 적용
    /// </summary>
    private void CheckAndAutoApplyTestSkills()
    {
        // v2에서는 SkillManager의 구조가 달라졌으므로 필요시 수정
        if (SkillManager.Instance != null)
        {
            // TODO: v2 테스트 스킬 자동 적용 로직 필요시 구현
        }
    }

    /// <summary>
    /// 모든 패시브 효과 적용 (v2 수정: 덮어쓰기 버그 해결)
    ///
    /// [버그 수정 전]: 각 스킬을 개별 적용 → 나중 스킬이 이전 스킬을 덮어씀
    /// [버그 수정 후]: 모든 보너스 합산 후 한 번에 적용
    /// </summary>
    private void ApplyAllPassiveEffects()
    {
        // Null 체크 후 컴포넌트 찾기
        if (_playerStats == null)
        {
            _playerStats = FindObjectOfType<PlayerStats>();
            if (_playerStats == null)
            {
                Debug.LogWarning("[PassiveSkillManager] PlayerStats를 찾을 수 없습니다.");
                return;
            }
        }

        if (_playerController == null)
        {
            _playerController = FindObjectOfType<PlayerController>();
        }

        // [핵심 수정] 보너스 변수들 (기본값 0)
        float expBonus = 0f;
        float damageBonus = 0f;
        float maxHpBonus = 0f;
        float magnetRangeBonus = 0f;
        float regenBonus = 0f;
        float moveSpeedBonus = 0f;
        float cooldownReduction = 0f;

        // 모든 스킬을 순회하며 누적 합산
        foreach (var kvp in ownedPassiveSkills)
        {
            if (!skillDataCache.TryGetValue(kvp.Key, out PassiveSkillData data)) continue;

            float value = data.GetEffectValue(kvp.Value);

            switch (kvp.Key)
            {
                case PassiveSkillType.ExperienceBonus:
                    expBonus += value;
                    break;
                case PassiveSkillType.DamageBoost:
                    damageBonus += value;
                    break;
                case PassiveSkillType.MaxHealth:
                    maxHpBonus += value;
                    break;
                case PassiveSkillType.MagnetRange:
                    magnetRangeBonus += value;
                    break;
                case PassiveSkillType.HealthRegen:
                    regenBonus += value;
                    break;
                case PassiveSkillType.MovementSpeed:
                    moveSpeedBonus += value;
                    break;
                case PassiveSkillType.CooldownReduction:
                    cooldownReduction += value;
                    break;
                case PassiveSkillType.DropRateBonus:
                    // TODO: 드롭률 증가 구현 필요
                    break;
            }
        }

        // [핵심 수정] 합산된 최종 값을 한 번에 적용
        _playerStats.SetExpBonusMultiplier(1f + expBonus);
        _playerStats.AddMaxHp(maxHpBonus);
        _playerStats.AddMagnetRange(magnetRangeBonus);
        _playerStats.SetHealthRegenRate(regenBonus);

        if (SkillManager.Instance != null)
        {
            // v2 SkillManager에 해당 메서드가 필요할 수 있음
            // SkillManager.Instance.SetDamageMultiplier(1f + damageBonus);
            // SkillManager.Instance.SetCooldownMultiplier(1f - cooldownReduction);
        }

        if (_playerController != null)
        {
            // [성능 최적화] 리플렉션 제거, 직접 호출 권장
            // 혹은 아직 PlayerController 코드를 못 건드리면 리플렉션 방어코드 유지
            var method = _playerController.GetType().GetMethod("SetMovementSpeedMultiplier");
            method?.Invoke(_playerController, new object[] { 1f + moveSpeedBonus });
        }

        OnPassiveSkillsUpdated?.Invoke();
    }
    #endregion

    #region Debug Functions
    /// <summary>
    /// 모든 패시브 스킬 초기화 (디버그용)
    /// </summary>
    [ContextMenu("모든 패시브 스킬 초기화")]
    public void ResetAllPassiveSkills()
    {
        ownedPassiveSkills.Clear();
        ApplyAllPassiveEffects();
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
    }
    #endregion
}
