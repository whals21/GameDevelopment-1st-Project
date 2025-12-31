using UnityEngine;
using System;

/// <summary>
/// 스킬 클래스에 타입 정보를 선언적으로 부여하여 SkillType과 매핑하는 속성
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SkillTypeAttribute : Attribute
{
    /// <summary>
    /// 이 스킬 클래스가 매핑되는 스킬 타입
    /// </summary>
    public SkillType Type { get; }

    /// <summary>
    /// 속성 초기화
    /// </summary>
    /// <param name="type">스킬 타입</param>
    public SkillTypeAttribute(SkillType type)
    {
        Type = type;
    }
}
