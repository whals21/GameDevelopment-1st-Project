using UnityEngine;

/// <summary>
/// 스킬 타입 열거형
/// 각 타입은 다른 동작 방식을 가집니다
/// </summary>
public enum SkillType
{
    Projectile,  // 투사체 스킬 (부메랑, 축구공 등)
    Guardian,    // 가디언 스킬 (플레이어 주변을 회전하며 공격)
    Aura,        // 오라 스킬 (지속 범위 효과)
    Drone,       // 드론 스킬 (자율 공격 유닛)
    Lightning,   // 번개 스킬 (무작위 적에게 번개 타격)
    RPG,         // RPG 스킬 (가장 가까운 적에게 로켓 발사)
    Special      // 특수 스킬 (진화 스킬 등)
}
