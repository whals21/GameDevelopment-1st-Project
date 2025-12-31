using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// SkillType에 따라 적절한 스킬 컴포넌트를 생성하는 팩토리 클래스
/// </summary>
public static class SkillFactory
{
    #region Skill Type Mapping
    private static Dictionary<SkillType, Type> _skillTypeMap;

    /// <summary>
    /// 스킬 타입 매핑 테이블 초기화
    /// 새 스킬 타입 추가 시 여기에 등록하세요.
    /// </summary>
    static SkillFactory()
    {
        _skillTypeMap = new Dictionary<SkillType, Type>
        {
            { SkillType.Projectile, typeof(ProjectileSkill) },
            { SkillType.Guardian, typeof(GuardianSkill) },
            { SkillType.Drone, typeof(DroneSkill) },
            { SkillType.Aura, typeof(AuraSkill) },
            { SkillType.Lightning, typeof(LightningSkill) },
            { SkillType.RPG, typeof(RPGSkill) },
            { SkillType.Spread, typeof(SpreadSkill) }
        };
    }
    #endregion

    #region Public API
    /// <summary>
    /// 새로운 스킬 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="owner">스킬이 부착될 게임 오브젝트</param>
    /// <param name="data">스킬 데이터</param>
    /// <param name="level">시작 레벨</param>
    /// <returns>생성된 스킬 인스턴스 (실패 시 null)</returns>
    public static SkillBase CreateSkill(GameObject owner, SkillData data, int level = 1)
    {
        if (owner == null || data == null)
        {
            Debug.LogError("[SkillFactory] Owner 또는 Data가 null입니다.");
            return null;
        }

        if (!_skillTypeMap.TryGetValue(data.skillType, out var componentType))
        {
            Debug.LogError($"[SkillFactory] 알 수 없는 스킬 타입: {data.skillType}");
            return null;
        }

        // 컴포넌트 추가
        Component comp = owner.AddComponent(componentType);
        SkillBase skill = comp as SkillBase;

        if (skill == null)
        {
            Debug.LogError($"[SkillFactory] {componentType.Name}가 SkillBase를 상속받지 않았습니다.");
            GameObject.Destroy(comp);
            return null;
        }

        // 스킬 초기화
        skill.Init(data, level);

        return skill;
    }

    /// <summary>
    /// 런타임에 새로운 스킬 타입을 등록합니다.
    /// 모드/플러그인 확장용입니다.
    /// </summary>
    /// <param name="skillType">스킬 타입</param>
    /// <param name="componentType">스킬 컴포넌트 타입 (SkillBase를 상속받아야 함)</param>
    /// <returns>등록 성공 여부</returns>
    public static bool RegisterSkillType(SkillType skillType, Type componentType)
    {
        if (componentType == null)
        {
            Debug.LogError("[SkillFactory] Component Type이 null입니다.");
            return false;
        }

        if (!typeof(SkillBase).IsAssignableFrom(componentType))
        {
            Debug.LogError($"[SkillFactory] {componentType.Name}가 SkillBase를 상속받지 않았습니다.");
            return false;
        }

        _skillTypeMap[skillType] = componentType;
        Debug.Log($"[SkillFactory] 스킬 타입 등록: {skillType} → {componentType.Name}");
        return true;
    }

    /// <summary>
    /// 현재 등록된 모든 스킬 타입을 반환합니다.
    /// </summary>
    public static IReadOnlyDictionary<SkillType, Type> GetAllRegisteredTypes()
    {
        return _skillTypeMap;
    }
    #endregion
}
