using UnityEngine;

/// <summary>
/// 스킬 데이터 컨테이너 (ScriptableObject)
/// 모든 스킬의 스탯, 레벨 데이터, 진화 정보를 담습니다.
///
/// [v2 리팩토링]
/// - 유산 코드(evoRequirements) 제거
/// - OnValidate로 데이터 무결성 검사 추가
/// - IsType() 헬퍼 메서드 추가
/// </summary>
[CreateAssetMenu(fileName = "New Skill", menuName = "Survivor/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    public string skillName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;

    [Header("스킬 타입")]
    public SkillType skillType = SkillType.Projectile;

    [Header("프리팹")]
    public GameObject prefab;

    [Header("기본 스탯")]
    [Tooltip("기본 데미지")]
    public float damage = 10f;
    [Tooltip("기본 쿨타임 (초)")]
    public float cooldown = 1f;
    [Tooltip("투사체/스킬 속도")]
    public float projectileSpeed = 15f;
    [Tooltip("한 번에 발사할 투사체 수")]
    public int projectileCount = 1;
    [Tooltip("공격 범위")]
    public float attackRange = 10f;

    [Header("투사체 설정 (Projectile)")]
    [Tooltip("투사체 확산 각도 (Spread, Soccer 등)")]
    [Range(0f, 180f)]
    public float spreadAngle = 30f;
    [Tooltip("투사체 이동 방식")]
    public ProjectileMovementType movementType = ProjectileMovementType.Straight;

    [Header("적 탐색 (Projectile/RPG)")]
    [Tooltip("적 탐색 반경 (미터)")]
    public float enemySearchRadius = 20f;

    [Header("소환체 설정 (Guardian/Drone)")]
    [Tooltip("호버링 반경 (미터)")]
    public float hoverRadius = 2f;
    [Tooltip("호버링 속도 (도/초)")]
    public float hoverSpeed = 90f;
    [Tooltip("호버링 타원 비율 (0~1, 1이면 정원)")]
    [Range(0f, 1f)]
    public float hoverEllipseRatio = 0.3f;
    [Tooltip("최소 공격 간격 (초) - Guardian 스킬은 연속 공격")]
    public float attackIntervalMin = 0.5f;

    [Header("특수 스킬 (Special)")]
    [Tooltip("다중 발사: 한 번에 발사할 투사체 수")]
    [Min(1)]
    public int specialProjectileCount = 1;
    [Tooltip("공전 투사체 여부")]
    public bool isOrbiting = false;
    [Tooltip("초기 각도 오프셋 (도)")]
    [Range(0f, 360f)]
    public float initialAngleOffset = 0f;

    [Header("번개 스킬 (Lightning)")]
    [Tooltip("번개 타격 간 딜레이 (초)")]
    public float strikeDelay = 0.15f;

    [Header("RPG 스킬 (RPG)")]
    [Tooltip("연사 속도 (초)")]
    public float rapidFireInterval = 0.1f;
    [Tooltip("기본 분산 각도")]
    public float defaultSpreadAngle = 15f;

    [Header("시각 효과 (v2)")]
    [Tooltip("투사체 스프라이트 (있으면 우선)")]
    public Sprite projectileSprite;
    public Color projectileColor = Color.white;
    [Range(0.1f, 5.0f)]
    public float projectileScale = 1f;

    [Header("오라 스킬 (Aura)")]
    [Tooltip("오라 데미지 간격 (초)")]
    public float damageInterval = 0.2f;

    [Header("레벨별 스탯 (최대 5레벨)")]
    [Tooltip("레벨별 스킬 스탯 (배열)")]
    public SkillLevel[] levels = new SkillLevel[5];

    [Header("진화 설정 (v2)")]
    [Tooltip("진화 후 획득할 스킬 데이터")]
    public SkillData evoSkill;
    [Tooltip("진화에 필요한 레벨 (0이면 기본값 5 사용)")]
    [Min(0)]
    public int evoRequiredLevel = 0;
    [Tooltip("진화에 필요한 패시브 스킬 (AND 조건 - 모두 보유 필요)")]
    public PassiveSkillType[] evoRequiredPassives;

    #region Helper Methods
    /// <summary>
    /// 현재 스킬이 특정 타입이 유효한지 확인합니다.
    /// </summary>
    public bool IsType(SkillType type)
    {
        return this.skillType == type;
    }
    #endregion

    #region Initialization
    private void OnValidate()
    {
        // 진화 데이터 무결성 검사 (자기 참조 방지)
        if (evoSkill == this)
        {
            Debug.LogWarning($"[SkillData] {skillName}은(는) 자기 자신을 진화할 수 없습니다. evoSkill 제거됨.");
            evoSkill = null;
        }

        // 레벨 배열 최소 크기 보장
        if (levels == null || levels.Length == 0)
        {
            levels = new SkillLevel[5];
        }

        // 참고: 능동 스킬은 보통 5레벨로 고정되므로 동적 배열 조정은 하지 않음
        // 필요시 PassiveSkillData처럼 maxLevel에 따라 동적 조정 가능
    }
    #endregion
}

/// <summary>
/// 스킬 레벨별 데이터
/// </summary>
[System.Serializable]
public class SkillLevel
{
    [Header("레벨 정보")]
    public int level = 1;

    [Header("스탯 배수")]
    [Range(0f, 5f)]
    public float damageMultiplier = 1f;
    [Range(0.1f, 2f)]
    public float cooldownMultiplier = 1f;

    [Header("추가 투사체")]
    public int additionalProjectiles = 0;

    [Header("투사체 속도 배수")]
    [Range(0.5f, 3f)]
    public float projectileSpeedMultiplier = 1f;

    [Header("투사체 크기 배수")]
    [Range(0.1f, 3f)]
    public float projectileSizeMultiplier = 1f;

    [Header("레벨업 시 크기 증가량")]
    public float projectileScaleIncrease = 0f;

    [Header("레벨업 설명")]
    public string upgradeDescription;
}
