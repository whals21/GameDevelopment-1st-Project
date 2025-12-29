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
/// Unity Inspector에서 레벨별 효과를 조정할 수 있습니다.
///
/// [v2 수정사항]
/// - maxLevel에 따라 effectLevels 배열이 자동으로 조절됩니다.
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
    [Tooltip("최대 레벨 개수에 맞춰 자동으로 배열이 늘어납니다. 백분율(%)은 소수점으로 표현 (예: 10% = 0.1f)")]
    public float[] effectLevels = new float[5]; // 기본값 5개지만 OnValidate에서 조정됨

    [Header("설정")]
    [Tooltip("이 스킬의 최대 레벨 (배열 크기가 이에 맞춰 자동 변경됨)")]
    public int maxLevel = 5;

    [Tooltip("스킬 획득 우선순위 (높을수록 우선 제시)")]
    public int priority = 0;

    [Tooltip("이미 최대 레벨일 때 다른 스킬 제시 확률 증가")]
    [Range(0f, 1f)]
    public float rerollChanceAtMaxLevel = 0.3f;

    /// <summary>
    /// 지정된 레벨의 효과 수치를 반환
    /// </summary>
    public float GetEffectValue(int level)
    {
        if (level <= 0 || level > effectLevels.Length)
        {
            Debug.LogWarning($"[PassiveSkillData] 잘못된 레벨 {level}. 기본값 0 반환.");
            return 0f;
        }
        return effectLevels[level - 1];
    }

    /// <summary>
    /// 현재 레벨에서 다음 레벨로 올라갈 때 증가하는 효과량을 반환
    /// </summary>
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
    public bool IsMaxLevel(int level)
    {
        return level >= maxLevel;
    }

    /// <summary>
    /// 효과 수치를 포맷팅된 문자열로 변환
    /// </summary>
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

    /// <summary>
    /// 데이터 값 유효성 검사 및 자동 초기화
    /// [v2 수정됨] maxLevel 변경 시 effectLevels 배열 크기 자동 조절
    /// </summary>
    private void OnValidate()
    {
        // 최소 레벨 보장
        if (maxLevel < 1) maxLevel = 1;

        // [수정된 로직] effectLevels 배열 크기가 maxLevel보다 작으면 자동 조절
        if (effectLevels == null || effectLevels.Length < maxLevel)
        {
            float[] newLevels = new float[maxLevel];

            // 기존 값 유지 (최대 레벨이 늘어나도 기존 데이터는 보존됨)
            if (effectLevels != null)
            {
                for (int i = 0; i < Mathf.Min(effectLevels.Length, maxLevel); i++)
                {
                    newLevels[i] = effectLevels[i];
                }
            }

            // 비어있는 새 슬롯(레벨)에 대해서는 기본값 설정
            for (int i = newLevels.Length - 1; i >= 0; i--)
            {
                if (newLevels[i] == 0f)
                {
                    float baseValue = GetBaseValueForType();
                    newLevels[i] = baseValue * (i + 1);
                }
            }

            effectLevels = newLevels;
        }

        // 우선순위 제한 (0~100)
        priority = Mathf.Clamp(priority, 0, 100);
    }

    private float GetBaseValueForType()
    {
        switch (passiveType)
        {
            case PassiveSkillType.ExperienceBonus:
                return 0.1f;
            case PassiveSkillType.DamageBoost:
                return 0.08f;
            case PassiveSkillType.MovementSpeed:
                return 0.15f;
            case PassiveSkillType.CooldownReduction:
                return 0.07f;
            case PassiveSkillType.DropRateBonus:
                return 0.12f;
            case PassiveSkillType.MagnetRange:
                return 0.5f;
            case PassiveSkillType.MaxHealth:
                return 10f;
            case PassiveSkillType.HealthRegen:
                return 0.5f;
            default:
                return 0.05f;
        }
    }
}
