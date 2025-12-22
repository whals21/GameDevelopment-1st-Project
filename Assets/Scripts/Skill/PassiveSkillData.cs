using UnityEngine;

/// <summary>
/// 패시브 스킬 타입 열거형
/// </summary>
public enum PassiveSkillType
{
    None,
    ExperienceBonus,     // 경험치 보너스
    MagnetRange,         // 경험치 구름 흡인 범위 증가
    DamageBoost,         // 공격력 증가
    MaxHealth,           // 최대 체력 증가
    HealthRegen,         // 체력 재생
    MovementSpeed,       // 이동속도 증가
    CooldownReduction,   // 쿨다운 감소
    DropRateBonus        // 아이템 드롭률 증가
}

/// <summary>
/// 패시브 스킬 데이터를 담는 ScriptableObject
/// Unity Inspector에서 레벨별 효과를 조정할 수 있음
/// </summary>
[CreateAssetMenu(fileName = "New Passive Skill", menuName = "Skills/Passive Skill Data")]
public class PassiveSkillData : ScriptableObject
{
    [Header("기본 정보")]
    public PassiveSkillType passiveType;
    public string skillName;
    [TextArea(3, 5)]
    public string description;
    public Sprite skillIcon;

    [Header("레벨별 효과 수치 (Inspector에서 조정 가능)")]
    // 레벨별 효과 수치 배열 (최대 5레벨)
    [Tooltip("레벨 1~5까지의 효과 수치. 백분율(%)은 소수점으로 표현 (예: 10% = 0.1f)")]
    public float[] effectLevels = new float[5];

    [Header("설정")]
    [Tooltip("이 스킬의 최대 레벨")]
    public int maxLevel = 5;

    [Tooltip("스킬 획득 우선순위 (높을수록 우선 제시)")]
    public int priority = 0;

    [Tooltip("이미 최대 레벨일 때 다른 스킬 제시 확률 증가")]
    [Range(0f, 1f)]
    public float rerollChanceAtMaxLevel = 0.3f;

    /// <summary>
    /// 지정된 레벨의 효과 수치를 반환
    /// </summary>
    /// <param name="level">스킬 레벨 (1-5)</param>
    /// <returns>효과 수치</returns>
    public float GetEffectValue(int level)
    {
        if (level <= 0 || level > effectLevels.Length)
        {
            Debug.LogWarning($"GetEffectValue: 잘못된 레벨 {level}. 기본값 0 반환.");
            return 0f;
        }

        return effectLevels[level - 1];
    }

    /// <summary>
    /// 현재 레벨에서 다음 레벨로 올라갈 때 증가하는 효과량을 반환
    /// </summary>
    /// <param name="currentLevel">현재 레벨</param>
    /// <returns>증가하는 효과량</returns>
    public float GetNextLevelBonus(int currentLevel)
    {
        if (currentLevel <= 0 || currentLevel >= maxLevel)
        {
            return 0f;
        }

        return GetEffectValue(currentLevel + 1) - GetEffectValue(currentLevel);
    }

    /// <summary>
    /// 스킬이 최대 레벨인지 확인
    /// </summary>
    /// <param name="level">확인할 레벨</param>
    /// <returns>최대 레벨이면 true</returns>
    public bool IsMaxLevel(int level)
    {
        return level >= maxLevel;
    }

    /// <summary>
    /// 효과 수치를 포맷팅된 문자열로 변환
    /// 예: "0.1f" -> "+10%"
    /// </summary>
    /// <param name="value">효과 수치</param>
    /// <returns>포맷팅된 문자열</returns>
    public string FormatEffectValue(float value)
    {
        switch (passiveType)
        {
            case PassiveSkillType.ExperienceBonus:
            case PassiveSkillType.DamageBoost:
            case PassiveSkillType.MovementSpeed:
            case PassiveSkillType.CooldownReduction:
            case PassiveSkillType.DropRateBonus:
                return $"+{value * 100:F0}%";

            case PassiveSkillType.MagnetRange:
                return $"범위 +{value:F1}m";

            case PassiveSkillType.MaxHealth:
                return $"+{value:F0} HP";

            case PassiveSkillType.HealthRegen:
                return $"초당 +{value:F1} HP";

            default:
                return $"+{value:F2}";
        }
    }

    /// <summary>
    /// 스킬 설명에 현재 레벨 효과를 포함하여 반환
    /// </summary>
    /// <param name="level">스킬 레벨</param>
    /// <returns>레벨이 포함된 설명</returns>
    public string GetLevelDescription(int level)
    {
        if (level <= 0 || level > effectLevels.Length)
        {
            return description;
        }

        float effectValue = effectLevels[level - 1];
        string effectText = FormatEffectValue(effectValue);

        return $"{description}\n\n현재 효과: {effectText}";
    }

    private void OnValidate()
    {
        // effectLevels 배열 크기 확인 및 자동 조정
        if (effectLevels == null || effectLevels.Length != 5)
        {
            float[] newLevels = new float[5];

            // 기존 값 유지
            if (effectLevels != null)
            {
                for (int i = 0; i < Mathf.Min(effectLevels.Length, 5); i++)
                {
                    newLevels[i] = effectLevels[i];
                }
            }

            // 기본값 설정
            for (int i = newLevels.Length - 1; i >= 0; i--)
            {
                if (newLevels[i] == 0f)
                {
                    // 기본 점진적 증가 패턴
                    float baseValue = GetBaseValueForType();
                    newLevels[i] = baseValue * (i + 1);
                }
            }

            effectLevels = newLevels;
        }

        // 최대 레벨 제한
        maxLevel = Mathf.Clamp(maxLevel, 1, 5);

        // 우선순위 제한
        priority = Mathf.Clamp(priority, 0, 100);
    }

    /// <summary>
    /// 패시브 스킬 타입별 기본 효과값 반환
    /// </summary>
    /// <returns>기본 효과값</returns>
    private float GetBaseValueForType()
    {
        switch (passiveType)
        {
            case PassiveSkillType.ExperienceBonus:
                return 0.1f;      // 10%
            case PassiveSkillType.DamageBoost:
                return 0.08f;     // 8%
            case PassiveSkillType.MovementSpeed:
                return 0.15f;     // 15%
            case PassiveSkillType.CooldownReduction:
                return 0.07f;     // 7%
            case PassiveSkillType.DropRateBonus:
                return 0.12f;     // 12%
            case PassiveSkillType.MagnetRange:
                return 0.5f;      // 0.5미터
            case PassiveSkillType.MaxHealth:
                return 10f;       // 10 HP
            case PassiveSkillType.HealthRegen:
                return 0.5f;      // 0.5 HP/초
            default:
                return 0.05f;     // 기본 5%
        }
    }
}