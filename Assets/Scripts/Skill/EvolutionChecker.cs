using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 진화 가능 여부 판정 유틸리티 (v2 리팩토링 - 최종 수정본)
///
/// v2 변경사항 (전문가 피드백 반영):
/// 1. 문자열 매칭 제거 -> SkillType 사용 (지역화 안전)
/// 2. 리플렉션 제거 -> SkillManager 공개 API 사용 (캡슐화 준수)
/// 3. 데이터 주도형 진화 로직 적용 (SkillData.evoSkill 기반)
/// 4. Type Mismatch 수정 -> fromSkillInstance (SkillBase) 추가
///
/// 사용법:
/// var evolutions = EvolutionChecker.GetAvailableEvolutions();
/// foreach (var evo in evolutions) { EvolutionChecker.ExecuteEvolution(evo.fromSkillInstance); }
/// </summary>
public static class EvolutionChecker
{
    #region Public API

    /// <summary>
    /// 현재 장착된 스킬 중 진화 가능한 스킬 목록을 반환합니다.
    /// 데이터 주도 설계로 모든 진화를 자동으로 탐지합니다.
    /// </summary>
    /// <returns>진화 가능한 스킬 목록</returns>
    public static List<EvolutionOption> GetAvailableEvolutions()
    {
        List<EvolutionOption> evolvableSkills = new List<EvolutionOption>();

        // 1. 필수 매니저 체크
        if (SkillManager.Instance == null)
        {
            Debug.LogWarning("[EvolutionChecker] SkillManager.Instance가 null입니다.");
            return evolvableSkills;
        }

        // 2. 모든 장착 스킬 순회 (SkillBase 인스턴스)
        IReadOnlyList<SkillBase> allSkills = SkillManager.Instance.GetAllSkills();
        foreach (var skill in allSkills)
        {
            if (skill == null || skill.Data == null) continue;

            SkillData currentSkill = skill.Data;
            int currentLevel = skill.CurrentLevel;

            // 3. 진화 데이터 체크
            if (currentSkill.evoSkill == null) continue;

            // 4. 진화 조건 확인
            if (CanEvolve(currentSkill, currentLevel))
            {
                evolvableSkills.Add(new EvolutionOption
                {
                    fromSkillInstance = skill,  // [수정] 인스턴스 저장 (SkillBase)
                    toSkill = currentSkill.evoSkill,  // 데이터 참조 (SkillData)
                    currentLevel = currentLevel,
                    requiredLevel = GetRequiredLevel(currentSkill),
                    fromSkillName = currentSkill.skillName,
                    toSkillName = currentSkill.evoSkill.skillName,
                    fromSkillType = currentSkill.skillType,
                    evolutionDescription = BuildEvolutionDescription(currentSkill, currentSkill.evoSkill)
                });
            }
        }

        return evolvableSkills;
    }

    /// <summary>
    /// 특정 스킬이 진화 가능한지 확인합니다.
    /// </summary>
    /// <param name="skillData">확인할 스킬 데이터</param>
    /// <param name="currentLevel">현재 레벨</param>
    /// <returns>진화 가능하면 true</returns>
    public static bool CanEvolve(SkillData skillData, int currentLevel)
    {
        if (skillData == null) return false;
        if (skillData.evoSkill == null) return false;

        // 1. 레벨 조건 확인
        int requiredLevel = GetRequiredLevel(skillData);
        if (currentLevel < requiredLevel) return false;

        // 2. 패시브 조건 확인 (필요시)
        if (!HasRequiredPassives(skillData)) return false;

        // 3. 추가 조건 확인 (evoRequirements 배열 기반)
        if (!MeetsCustomRequirements(skillData)) return false;

        return true;
    }

    /// <summary>
    /// 진화를 실행합니다.
    /// </summary>
    /// <param name="fromSkillInstance">현재 장착된 스킬 인스턴스</param>
    /// <returns>성공 여부</returns>
    public static bool ExecuteEvolution(SkillBase fromSkillInstance)
    {
        if (fromSkillInstance == null || fromSkillInstance.Data == null)
        {
            Debug.LogWarning("[EvolutionChecker] 진화할 스킬이 없습니다.");
            return false;
        }

        SkillData currentData = fromSkillInstance.Data;
        SkillData targetData = currentData.evoSkill;

        if (targetData == null)
        {
            Debug.LogWarning("[EvolutionChecker] 진화 대상 데이터가 없습니다.");
            return false;
        }

        if (SkillManager.Instance == null)
        {
            Debug.LogError("[EvolutionChecker] SkillManager.Instance를 찾을 수 없습니다.");
            return false;
        }

        // 현재 레벨 유지 (진화 후에도 레벨 유지됨)
        int currentLevel = SkillManager.Instance.GetSkillLevel(currentData);

        // 진화 실행 (SkillManager에게 위임)
        SkillManager.Instance.ReplaceSkill(currentData, targetData, currentLevel);

        Debug.Log($"[EvolutionChecker] 진화 완료: {currentData.skillName} → {targetData.skillName}");
        return true;
    }

    #endregion

    #region Private Methods - 조건 확인

    /// <summary>
    /// 진화에 필요한 레벨을 반환합니다.
    /// SkillData.evoRequiredLevel을 사용하며, 0이면 기본값 5를 반환합니다.
    /// </summary>
    private static int GetRequiredLevel(SkillData skillData)
    {
        if (skillData.evoRequiredLevel > 0)
        {
            return skillData.evoRequiredLevel;
        }
        return 5; // 기본값
    }

    /// <summary>
    /// 필요한 패시브 스킬을 보유하고 있는지 확인합니다.
    /// v2: SkillData.evoRequiredPassives(PassiveSkillType[])를 직접 사용합니다.
    /// </summary>
    private static bool HasRequiredPassives(SkillData skillData)
    {
        // evoRequiredPassives 체크
        if (skillData.evoRequiredPassives != null && skillData.evoRequiredPassives.Length > 0)
        {
            if (PassiveSkillManager.Instance == null)
            {
                Debug.LogWarning("[EvolutionChecker] PassiveSkillManager.Instance가 null입니다.");
                return false;
            }

            foreach (var passiveType in skillData.evoRequiredPassives)
            {
                if (!PassiveSkillManager.Instance.HasSkill(passiveType))
                {
                    return false;
                }
            }
        }

        return true; // 패시브 조건 없음 또는 모두 충족
    }

    /// <summary>
    /// 커스텀 진화 조건을 확인합니다.
    /// 추후 확장 가능: 특정 아이템 보유, 특정 스킬 조합 등.
    /// </summary>
    private static bool MeetsCustomRequirements(SkillData skillData)
    {
        // [확장 가능] 추후 다양한 조건 추가
        // 예: 특정 스킬과의 조합, 아이템 보유 여부 등

        return true;
    }

    /// <summary>
    /// 진화 설명 텍스트를 생성합니다.
    /// </summary>
    private static string BuildEvolutionDescription(SkillData from, SkillData to)
    {
        int requiredLevel = GetRequiredLevel(from);

        string desc = $"Lv{requiredLevel}에서 {to.skillName}(으)로 진화";

        // 패시브 조건이 있으면 추가
        List<string> passiveNames = new List<string>();

        if (from.evoRequiredPassives != null && from.evoRequiredPassives.Length > 0)
        {
            foreach (var passiveType in from.evoRequiredPassives)
            {
                passiveNames.Add(passiveType.ToString());
            }
        }

        if (passiveNames.Count > 0)
        {
            desc += $"\n필요 패시브: {string.Join(", ", passiveNames)}";
        }

        return desc;
    }

    #endregion
}

#region 진화 관련 데이터 구조

/// <summary>
/// 진화 가능한 스킬 옵션 정보
/// UI에서 진화 선택지를 표시할 때 사용합니다.
/// </summary>
[System.Serializable]
public class EvolutionOption
{
    [Header("스킬 참조")]
    public SkillBase fromSkillInstance; // [수정] 실제 장착된 스킬 인스턴스 (진화 실행용)
    public SkillData toSkill;            // 진화 후 스킬 데이터

    [Header("레벨 정보")]
    public int currentLevel;         // 현재 레벨
    public int requiredLevel;        // 필요 레벨

    [Header("표시용 데이터")]
    public string fromSkillName;     // 진화 전 이름
    public string toSkillName;       // 진화 후 이름
    public SkillType fromSkillType;  // 스킬 타입
    public string evolutionDescription; // 진화 조건 설명

    /// <summary>
    /// 진화 가능한지 여부 (레벨 조건 충족 여부)
    /// </summary>
    public bool CanEvolve => currentLevel >= requiredLevel;
}

/// <summary>
/// 진화 타입 열거형 (v2)
/// v1과 달리 데이터 주도 설계로 불필요해졌으나,
/// UI에서 아이콘/효과 구분을 위해 유지.
/// </summary>
public enum EvolutionType
{
    MagneticDart,   // 부메랑 → 마그네틱 다트
    DeathFromAbove, // RPG → 데스 프롬 어보브 (추후 예정)
    // 필요시 추가
}

#endregion
