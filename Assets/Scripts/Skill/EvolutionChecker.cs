using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 스킬 진화 조건 체커
/// 특정 스킬의 진화 조건을 확인하고 진화 가능 여부를 반환
/// </summary>
public static class EvolutionChecker
{
    /// <summary>
    /// 마그네틱 다트 진화 가능 여부 확인
    /// 조건: 부메랑 Lv5 + 자석 범위 증가 패시브 보유
    /// </summary>
    /// <returns>진화 가능하면 true</returns>
    public static bool CanEvolveToMagneticDart()
    {
        // 1. SkillManager 확인
        if (SkillManager.Instance == null)
        {
            Debug.LogWarning("[EvolutionChecker] SkillManager.Instance가 null입니다.");
            return false;
        }

        // 2. 부메랑 스킬 슬롯 찾기
        int boomerangSlot = FindBoomerangSlot();
        if (boomerangSlot < 0)
        {
            return false;
        }

        // 3. 부메랑 스킬 레벨 확인 (Lv5 이상 필요)
        int boomerangLevel = SkillManager.Instance.GetSkillLevel(boomerangSlot);
        if (boomerangLevel < 5)
        {
            return false;
        }

        // 4. 자석 범위 패시브 보유 확인
        if (PassiveSkillManager.Instance == null)
        {
            Debug.LogWarning("[EvolutionChecker] PassiveSkillManager.Instance가 null입니다.");
            return false;
        }

        if (!PassiveSkillManager.Instance.HasSkill(PassiveSkillType.MagnetRange))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 부메랑 스킬이 장착된 슬롯 찾기
    /// </summary>
    /// <returns>부메랑 슬롯 인덱스, 없으면 -1</returns>
    private static int FindBoomerangSlot()
    {
        // 모든 스킬 슬롯 확인
        for (int i = 0; i < 5; i++)
        {
            if (SkillManager.Instance.HasSkill(i))
            {
                // SkillData를 가져와서 부메랑인지 확인
                // (SkillManager에 GetSkillData 메서드가 필요할 수 있음)
                // 현재는 이름으로 체크하거나 다른 방법 필요
                SkillData skill = GetSkillDataAtSlot(i);
                if (skill != null)
                {
                    // 스킬 이름이나 타입으로 부메랑 확인
                    if (skill.name.Contains("Boomerang") ||
                        skill.skillName.Contains("Boomerang") ||
                        skill.name.Contains("부메랑") ||
                        skill.skillName.Contains("부메랑"))
                    {
                        return i;
                    }
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// 특정 슬롯의 SkillData 가져오기
    /// </summary>
    private static SkillData GetSkillDataAtSlot(int slot)
    {
        // SkillManager에 내부 배열에 접근하는 방법이 필요
        // 현재로는 리플렉션을 사용하거나 SkillManager에 public 메서드 추가 필요

        // 임시로 리플렉션 사용 (권장하지 않음)
        var skillManager = SkillManager.Instance;
        if (skillManager == null) return null;

        var field = skillManager.GetType().GetField("equippedSkills", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            var skills = field.GetValue(skillManager) as SkillData[];
            if (skills != null && slot >= 0 && slot < skills.Length)
            {
                return skills[slot];
            }
        }

        return null;
    }

    /// <summary>
    /// 진화 가능한 모든 스킬 목록 반환
    /// </summary>
    /// <returns>진화 가능한 스킬 타입 배열</returns>
    public static EvolvableSkill[] GetAvailableEvolutions()
    {
        List<EvolvableSkill> evolvableSkills = new List<EvolvableSkill>();

        // 마그네틱 다트 확인
        if (CanEvolveToMagneticDart())
        {
            evolvableSkills.Add(new EvolvableSkill
            {
                fromSkillName = "Boomerang",
                toSkillName = "MagneticDart",
                evolutionType = EvolutionType.MagneticDart
            });
        }

        return evolvableSkills.ToArray();
    }
}

/// <summary>
/// 진화 가능한 스킬 정보
/// </summary>
public class EvolvableSkill
{
    public string fromSkillName;     // 원본 스킬 이름
    public string toSkillName;       // 진화 후 스킬 이름
    public EvolutionType evolutionType; // 진화 타입
}

/// <summary>
/// 진화 타입 열거형
/// </summary>
public enum EvolutionType
{
    MagneticDart,  // 부메랑 → 마그네틱 다트
    // 추후 추가 예정
    // DeathFromAbove,  // RPG → 데스 프롬 어보브
    // etc.
}
